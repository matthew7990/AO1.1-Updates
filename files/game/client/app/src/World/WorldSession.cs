using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Argentum.Client.Network;
using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.World;

public sealed class WorldSession
{
    public int MapId { get; set; }
    public int MapResource { get; set; }
    public int UserIndex { get; set; }
    public int CharIndex { get; set; }
    public string CharacterName { get; set; } = "";
    public int Privilege { get; set; }
    public int TileX { get; set; }
    public int TileY { get; set; }
    public int Body { get; set; }
    public int Head { get; set; }
    public int GfxWeapon { get; set; }
    public int GfxShield { get; set; }
    public int GfxHelmet { get; set; }
    public int MinHp { get; set; }
    public int MaxHp { get; set; }
    public int MinMana { get; set; }
    public int MaxMana { get; set; }
    public int MinSta { get; set; }
    public int MaxSta { get; set; }
    public int Shield { get; set; }
    public int Gold { get; set; }
    public int GoldPerLevel { get; set; }
    public int Exp { get; set; }
    public int ExpNext { get; set; }
    public int ClassId { get; set; }
    public bool IsDead { get; set; }
    public int Level { get; set; }
    public PlayerInventory Inventory { get; } = new();
    public SpellBook Spells { get; } = new();
    public SpellHotbar SpellHotbar { get; } = new();
    public CharacterFx SelfFx { get; } = new();
    public int UsingSkill { get; set; }
    public bool AreaSpellCast { get; set; }
    public int AreaSpellRadio { get; set; }
    public int MagicIntervalMs { get; set; } = 1200;
    public CsmMap? Map { get; set; }
    public MapsWorldCatalog? MapsWorld { get; set; }
    public bool LoggedIn { get; set; }
    public Dictionary<(int X, int Y), byte> Blocks { get; } = new();
    public int Heading { get; set; } = 3;
    public int WalkIntervalMs { get; set; } = 200;
    public int AttackIntervalMs { get; set; } = 1200;
    public DateTime WarpGraceUntil { get; set; }
    public CharacterMotion Motion { get; } = new();
    public WorldCharacters Characters { get; } = new();
    public bool StepPhase { get; set; }

    public WarpPreviewState WarpPreview { get; } = new();
    public bool WarpAwaitingPosition { get; set; }
    /// <summary>True tras un warp a pie: no reiniciar motion ni reproducir footstep de spawn.</summary>
    public bool SeamlessWarpActive { get; set; }

    private readonly AdjacentMapRing _adjacentRing = new();
    private readonly List<AdjacentMapRing.NeighborSlice> _peekSlices = new();
    private MapWarpCutIndex? _warpCuts;

    public IReadOnlyList<MapWarpCutIndex.WarpCut> WarpCuts => _warpCuts?.Cuts ?? Array.Empty<MapWarpCutIndex.WarpCut>();

    public AdjacentPeekLiveState AdjacentPeekLive { get; } = new();
    public RoofFadeState RoofFade { get; } = new();

    /// <summary>mapId, srcMinX, srcMinY, srcMaxX, srcMaxY</summary>
    public Func<int, int, int, int, int, Task>? RequestAdjacentPeekAsync { get; set; }

    public const int WarpGraceMs = 400;

    public void LoadMap(int mapId, int resourceId = 0)
    {
        MapId = mapId;
        MapResource = resourceId > 0 ? resourceId : mapId;
        MapDiagnostics.Log($"SetMap id={mapId} resource={MapResource}");
        Map = MapLoader.TryLoad(mapId, MapResource);
        Characters.Clear();
        RoofFade.ResetForMap(Map);
        _warpCuts = Map is not null ? MapWarpCutIndex.Build(Map, MapId, MapsWorld) : null;
        if (_warpCuts is not null)
        {
            _adjacentRing.PreloadFromCuts(_warpCuts);
        }
        _peekSlices.Clear();
    }

    public void SetMap(int mapId, int resourceId = 0) => LoadMap(mapId, resourceId);

    public void ApplyChangeMap(int mapId, int resourceId)
    {
        LoadMap(mapId, resourceId);
        Blocks.Clear();
    }

    /// <summary>VB6 SwitchMap + paso en curso: recarga mapa sin cortar scroll del tile engine.</summary>
    public bool TryBeginSeamlessWarp(int mapId, int resourceId)
    {
        WarpPreview.EnsureLoaded(mapId, resourceId);
        if (WarpPreview.Map is null)
        {
            return false;
        }

        var spawnX = WarpPreview.SpawnX;
        var spawnY = WarpPreview.SpawnY;
        if (spawnX < 1 || spawnY < 1)
        {
            return false;
        }

        var heading = WarpPreview.EntryHeading > 0 ? WarpPreview.EntryHeading : Heading;
        var keepMotion = Motion.IsMoving;
        MovementDiagnostics.LogSession(this, "WARP_SEAMLESS",
            $"map={mapId} spawn=({spawnX},{spawnY}) keepMotion={keepMotion} heading={heading}");

        LoadMap(mapId, resourceId);
        Blocks.Clear();
        Heading = heading;
        SeamlessWarpActive = true;
        WarpAwaitingPosition = false;

        if (keepMotion)
        {
            TileX = spawnX;
            TileY = spawnY;
        }
        else
        {
            ApplyEntryStep(spawnX, spawnY, heading);
        }

        WarpGraceUntil = DateTime.UtcNow.AddMilliseconds(WarpGraceMs);
        WarpPreview.Clear();
        return true;
    }

    /// <summary>Spawn del servidor tras ChangeMap (fallback o corrección).</summary>
    public void ApplyWarpArrival(int spawnX, int spawnY)
    {
        if (spawnX < 1 || spawnY < 1)
        {
            return;
        }

        var heading = WarpPreview.EntryHeading > 0 ? WarpPreview.EntryHeading : Heading;
        MovementDiagnostics.LogSession(this, "WARP_ARRIVE",
            $"spawn=({spawnX},{spawnY}) from=({TileX},{TileY}) moving={Motion.IsMoving}");

        if (Motion.IsMoving)
        {
            TileX = spawnX;
            TileY = spawnY;
        }
        else if (spawnX == TileX && spawnY == TileY)
        {
            // ya en destino
        }
        else if (Math.Abs(spawnX - TileX) <= 1 && Math.Abs(spawnY - TileY) <= 1)
        {
            Motion.BeginStep(TileX, TileY, spawnX, spawnY);
            TileX = spawnX;
            TileY = spawnY;
        }
        else
        {
            ApplyEntryStep(spawnX, spawnY, heading);
        }

        Heading = heading;
        SeamlessWarpActive = true;
        WarpAwaitingPosition = false;
        WarpGraceUntil = DateTime.UtcNow.AddMilliseconds(WarpGraceMs);
        WarpPreview.Clear();
    }

    private void ApplyEntryStep(int spawnX, int spawnY, int heading)
    {
        var (dx, dy) = HeadingToDelta(heading);
        var entryX = spawnX - dx;
        var entryY = spawnY - dy;
        if (entryX is >= 1 and <= CsmMap.PlayableSize
            && entryY is >= 1 and <= CsmMap.PlayableSize)
        {
            Motion.BeginStep(entryX, entryY, spawnX, spawnY);
        }
        TileX = spawnX;
        TileY = spawnY;
    }

    public string? DeathMessage { get; set; }
    public string? GameMessage { get; set; }
    public bool Meditating { get; set; }
    public bool Hidden { get; set; }
    public string? CommerceVendor { get; set; }
    public bool CommerceOpen { get; set; }
    public NpcCommerceInventory NpcCommerce { get; } = new();
    public GameConsole Console { get; } = new();
    public CharacterDialogs Dialogs { get; } = new();
    public CharacterFloatingTexts FloatingTexts { get; } = new();
    public int AliveBody { get; set; }
    public int AliveHead { get; set; }

    public void ApplyDeathAppearance()
    {
        if (!IsDead)
        {
            return;
        }
        if (Body > 0 && Body != CharacterChangeReader.CasperBodyIdle)
        {
            AliveBody = Body;
            AliveHead = Head;
        }
        Body = CharacterChangeReader.CasperBodyIdle;
        Head = 0;
        GfxWeapon = 0;
        GfxShield = 0;
        GfxHelmet = 0;
    }

    public bool PredictWalk(int heading)
    {
        var (dx, dy) = HeadingToDelta(heading);
        var newX = TileX + dx;
        var newY = TileY + dy;
        // VB6: LegalWalk rechaza coords fuera de 1..100; el cambio de mapa es por TileExit al pisar la salida.
        if (newX < 1 || newY < 1 || newX > CsmMap.PlayableSize || newY > CsmMap.PlayableSize)
        {
            Heading = heading;
            MovementDiagnostics.LogSession(this, "PREDICT_FAIL", $"heading={heading} fuera de mapa -> ({newX},{newY})");
            return false;
        }
        if (Map is null)
        {
            Heading = heading;
            MovementDiagnostics.LogSession(this, "PREDICT_FAIL", $"heading={heading} sin mapa cargado");
            return false;
        }
        if (!CanWalkTo(newX, newY, heading))
        {
            Heading = heading;
            MovementDiagnostics.LogSession(this, "PREDICT_FAIL", $"heading={heading} bloqueado -> ({newX},{newY})");
            return false;
        }
        var destTile = Map.Tiles[newX, newY];
        if (destTile.ExitMap > 0 && !IsDead && DateTime.UtcNow < WarpGraceUntil)
        {
            Heading = heading;
            MovementDiagnostics.LogSession(this, "PREDICT_FAIL", $"heading={heading} salida en grace -> ({newX},{newY}) exit={destTile.ExitMap}");
            return false;
        }
        Heading = heading;
        var fromX = TileX;
        var fromY = TileY;
        Motion.BeginStep(TileX, TileY, newX, newY);
        TileX = newX;
        TileY = newY;
        MovementDiagnostics.LogSession(this, "PREDICT_OK",
            $"heading={heading} ({fromX},{fromY})->({newX},{newY}) exit={destTile.ExitMap}");
        if (destTile.ExitMap > 0)
        {
            WarpPreview.StageExit(destTile.ExitMap, destTile.ExitX, destTile.ExitY, heading, newX, newY);
        }
        return true;
    }

    /// <summary>Franjas de mapas adyacentes precargados visibles según posición del PJ.</summary>
    public IReadOnlyList<AdjacentMapRing.NeighborSlice> GetAdjacentPeekSlices(Vector2 viewport)
    {
        _peekSlices.Clear();
        if (Map is null || _warpCuts is null)
        {
            return _peekSlices;
        }

        var camera = WorldCamera.Create(this, viewport);
        if (!CameraTouchesMapEdge(camera))
        {
            return _peekSlices;
        }

        var fillMargin = WarpPreviewState.MotionMarginTiles(Motion) + 8;
        _warpCuts.CollectPeekSlices(TileX, TileY, camera, viewport, fillMargin, _peekSlices);
        TryRequestAdjacentPeekLive(_peekSlices);
        return _peekSlices;
    }

    private void TryRequestAdjacentPeekLive(IReadOnlyList<AdjacentMapRing.NeighborSlice> slices)
    {
        if (RequestAdjacentPeekAsync is null || slices.Count == 0)
        {
            return;
        }

        var signature = ComputePeekRequestSignature(slices);
        var now = System.Environment.TickCount64;
        if (!AdjacentPeekLive.ShouldRequest(signature, now))
        {
            return;
        }

        AdjacentPeekLive.MarkRequested(signature, now);
        foreach (var slice in slices)
        {
            _ = RequestAdjacentPeekAsync(
                slice.Map.MapId,
                slice.SrcMinX,
                slice.SrcMinY,
                slice.SrcMaxX,
                slice.SrcMaxY);
        }
    }

    private static ulong ComputePeekRequestSignature(IReadOnlyList<AdjacentMapRing.NeighborSlice> slices)
    {
        ulong sig = 0;
        foreach (var slice in slices)
        {
            sig = Hash(sig, slice.Map.MapId);
            sig = Hash(sig, slice.SrcMinX);
            sig = Hash(sig, slice.SrcMaxX);
            sig = Hash(sig, slice.SrcMinY);
            sig = Hash(sig, slice.SrcMaxY);
        }
        return sig;
    }

    private static ulong Hash(ulong seed, int value) => seed * 31 + (uint)value;

    private static bool CameraTouchesMapEdge(WorldCamera camera) =>
        camera.MinBufferedX <= CsmMap.MinMapTile
        || camera.MaxBufferedX >= CsmMap.MaxMapTile
        || camera.MinBufferedY <= CsmMap.MinMapTile
        || camera.MaxBufferedY >= CsmMap.MaxMapTile;

    public bool IsOnExitTile() =>
        Map is not null
        && TileX is >= CsmMap.MinMapTile and <= CsmMap.MaxMapTile
        && TileY is >= CsmMap.MinMapTile and <= CsmMap.MaxMapTile
        && Map.Tiles[TileX, TileY].ExitMap > 0;

    public bool CanWalkTo(int x, int y, int heading)
    {
        if (x < 1 || y < 1 || x > CsmMap.PlayableSize || y > CsmMap.PlayableSize)
        {
            return false;
        }
        if (!IsDead)
        {
            foreach (var ch in Characters.All)
            {
                if (ch.TileX == x && ch.TileY == y)
                {
                    return false;
                }
            }
        }
        if (Map is null)
        {
            return false;
        }
        var tile = Map.Tiles[x, y];
        var blocked = tile.Blocked;
        if (Blocks.TryGetValue((x, y), out var synced))
        {
            blocked = synced;
        }
        if (blocked == 0)
        {
            return true;
        }
        var mask = 1 << (heading - 1);
        return (blocked & mask) == 0;
    }

    public void ConfirmMove(int newX, int newY)
    {
        if (newX == TileX && newY == TileY)
        {
            MovementDiagnostics.LogSession(this, "CONFIRM_SKIP", $"servidor confirma ({newX},{newY}) ya predicho");
            return;
        }
        var oldX = TileX;
        var oldY = TileY;
        if (System.Math.Abs(newX - oldX) > 1 || System.Math.Abs(newY - oldY) > 1)
        {
            MovementDiagnostics.LogSession(this, "CONFIRM_TELEPORT",
                $"servidor ({newX},{newY}) desde ({oldX},{oldY}) -> SnapPosition");
            SnapPosition(newX, newY);
            return;
        }
        if (!Motion.IsMoving)
        {
            MovementDiagnostics.LogSession(this, "CONFIRM_LATE",
                $"servidor ({newX},{newY}) desde ({oldX},{oldY}) animación ya terminó -> BeginStep");
            Motion.BeginStep(oldX, oldY, newX, newY);
        }
        else
        {
            MovementDiagnostics.LogSession(this, "CONFIRM_ADJUST",
                $"servidor ({newX},{newY}) desde ({oldX},{oldY}) en animación");
        }
        TileX = newX;
        TileY = newY;
        UpdateHeadingFromDelta(newX - oldX, newY - oldY);
    }

    public void SnapPosition(int x, int y)
    {
        MovementDiagnostics.LogSession(this, "SNAP",
            $"({TileX},{TileY})->({x},{y})");
        TileX = x;
        TileY = y;
        Motion.Reset();
    }

    public void ApplyPosUpdate(int x, int y)
    {
        if (x == TileX && y == TileY)
        {
            MovementDiagnostics.LogSession(this, "POSUPDATE_SKIP", $"servidor ({x},{y}) sin cambio");
            return;
        }
        var wasMoving = Motion.IsMoving;
        MovementDiagnostics.LogSession(this, "POSUPDATE",
            $"servidor corrige ({TileX},{TileY})->({x},{y}) moving={wasMoving} reset={wasMoving}");
        TileX = x;
        TileY = y;
        if (wasMoving)
        {
            Motion.Reset();
        }
    }

    private void UpdateHeadingFromDelta(int dx, int dy)
    {
        if (dx > 0)
        {
            Heading = 2;
        }
        else if (dx < 0)
        {
            Heading = 4;
        }
        else if (dy > 0)
        {
            Heading = 3;
        }
        else if (dy < 0)
        {
            Heading = 1;
        }
    }

    public static (int Dx, int Dy) HeadingToDelta(int heading) => heading switch
    {
        1 => (0, -1),
        2 => (1, 0),
        3 => (0, 1),
        4 => (-1, 0),
        _ => (0, 0),
    };
}
