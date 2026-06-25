using System;
using System.Collections.Generic;
using Argentum.Client.Core;
using Argentum.Client.Resources;
using Argentum.Client.Ui;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client;

public partial class WorldView : Node2D
{
    private WorldSession? _session;
    private GameResources? _resources;
    private bool _loginPreview;
    private Vector2 _loginPreviewScreen;
    private CsmMap? _drawMap;
    private readonly WarpPeekTileCache _peekCache = new();
    private static readonly CharacterMotion PeekNpcMotion = new();


    public void Bind(WorldSession session, GameResources? resources)
    {
        _session = session;
        _resources = resources;
        QueueRedraw();
    }

    public void SetLoginPreview(bool enabled, Vector2 screenSize, Vector2 offset = default)
    {
        _loginPreview = enabled;
        Position = enabled ? offset : Vector2.Zero;
        _loginPreviewScreen = enabled ? screenSize : Vector2.Zero;
        QueueRedraw();
    }

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Process(double delta)
    {
        if (_session is null)
        {
            return;
        }
        _session.Motion.Advance(delta);
        _session.Dialogs.PruneExpired();
        _session.FloatingTexts.PruneExpired();
        if (_session.Map is not null
            && _session.TileX is >= CsmMap.MinMapTile and <= CsmMap.MaxMapTile
            && _session.TileY is >= CsmMap.MinMapTile and <= CsmMap.MaxMapTile)
        {
            var playerTrigger = _session.Map.Tiles[_session.TileX, _session.TileY].Trigger;
            _session.RoofFade.Advance(delta, playerTrigger);
        }
        PruneCharacterFx(_session.SelfFx);
        foreach (var ch in _session.Characters.All)
        {
            PruneCharacterFx(ch.Fx);
        }
        foreach (var character in _session.Characters.All)
        {
            character.Motion.Advance(delta);
        }
        if (_session.SeamlessWarpActive && !_session.Motion.IsMoving)
        {
            _session.SeamlessWarpActive = false;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_session is null)
        {
            return;
        }

        var screen = _loginPreview ? _loginPreviewScreen : GameViewport.LogicalSize;
        DrawRect(new Rect2(0, 0, screen.X, screen.Y), new Color(0.02f, 0.02f, 0.04f));

        if (_session.Map is null)
        {
            var camera = WorldCamera.Create(_session, screen);
            DrawCharacter(screen, camera);
            return;
        }

        var slices = _session.GetAdjacentPeekSlices(screen);
        DrawWorldMap(screen, _session.Map, _session.TileX, _session.TileY, drawCharacters: true, peekSlices: slices);

        if (PeekDiagnostics.Enabled)
        {
            DrawPeekDebugOverlay(screen, slices);
        }
    }

    private void EnsurePeekCache(Vector2 screen, IReadOnlyList<AdjacentMapRing.NeighborSlice> slices)
    {
        if (slices.Count == 0)
        {
            return;
        }
        var motion = _session!.Motion;
        var camera = WorldCamera.Create(_session, screen);
        var fillMargin = WarpPreviewState.MotionMarginTiles(motion) + 4;
        if (!_peekCache.IsCurrent(_session.MapId, _session.TileX, _session.TileY, camera, slices, _session.AdjacentPeekLive.Version))
        {
            _peekCache.Rebuild(_session.MapId, _session.TileX, _session.TileY, camera, fillMargin, slices, _resources?.Npcs, _session.AdjacentPeekLive);
        }
    }

    private void DrawWorldMap(
        Vector2 screen,
        CsmMap map,
        int centerTileX,
        int centerTileY,
        bool drawCharacters,
        IReadOnlyList<AdjacentMapRing.NeighborSlice>? peekSlices = null)
    {
        _drawMap = map;
        var camera = WorldCamera.Create(_session!, screen, centerTileX, centerTileY);
        var hasPeek = peekSlices is { Count: > 0 };
        if (hasPeek)
        {
            EnsurePeekCache(screen, peekSlices!);
        }

        DrawTileLayer(camera, 0, camera.MinX, camera.MaxX, camera.MinY, camera.MaxY, buffered: false);

        DrawTileLayer(camera, 1, camera.MinBufferedX, camera.MaxBufferedX, camera.MinBufferedY, camera.MaxBufferedY, buffered: true);
        if (hasPeek)
        {
            // L0 peek después de L1 main: los gráficos anchos de L1 tapaban el piso en la franja negra.
            DrawPeekTileLayer(camera, 0, buffered: false, screen);
            DrawPeekTileLayer(camera, 1, buffered: true, screen);
        }

        DrawObjects(camera, camera.MinBufferedX, camera.MaxBufferedX, camera.MinBufferedY, camera.MaxBufferedY, afterLayer2: true);
        if (hasPeek)
        {
            DrawPeekObjects(camera, afterLayer2: true, screen);
        }

        if (drawCharacters)
        {
            for (var y = camera.MinBufferedY; y <= camera.MaxBufferedY; y++)
            {
                DrawCharacterBodiesAtRow(y, screen, camera, centerTileX, centerTileY);
                DrawObjects(camera, camera.MinBufferedX, camera.MaxBufferedX, y, y, afterLayer2: false);
                if (hasPeek)
                {
                    DrawPeekObjects(camera, afterLayer2: false, screen, rowY: y);
                }
                DrawTileLayer(camera, 2, camera.MinBufferedX, camera.MaxBufferedX, y, y, buffered: true);
                if (hasPeek)
                {
                    DrawPeekTileLayer(camera, 2, buffered: true, screen, rowY: y);
                    DrawPeekCharacterBodiesAtRow(y, screen, camera);
                }
                DrawCharacterHeadsAtRow(y, screen, camera, centerTileX, centerTileY);
                if (hasPeek)
                {
                    DrawPeekCharacterHeadsAtRow(y, screen, camera);
                }
            }
        }
        else
        {
            for (var y = camera.MinBufferedY; y <= camera.MaxBufferedY; y++)
            {
                DrawObjects(camera, camera.MinBufferedX, camera.MaxBufferedX, y, y, afterLayer2: false);
                if (hasPeek)
                {
                    DrawPeekObjects(camera, afterLayer2: false, screen, rowY: y);
                }
                DrawTileLayer(camera, 2, camera.MinBufferedX, camera.MaxBufferedX, y, y, buffered: true);
                if (hasPeek)
                {
                    DrawPeekTileLayer(camera, 2, buffered: true, screen, rowY: y);
                }
            }
        }

        DrawRoofLayer(camera, camera.MinBufferedX, camera.MaxBufferedX, camera.MinBufferedY, camera.MaxBufferedY, buffered: true);
        if (hasPeek)
        {
            DrawPeekTileLayer(camera, 3, buffered: true, screen);
        }
        _drawMap = null;
    }

    private void DrawPeekTileLayer(WorldCamera camera, int layerIndex, bool buffered, Vector2 screen, int? rowY = null)
    {
        var screenRect = new Rect2(0, 0, screen.X, screen.Y);
        foreach (var cell in _peekCache.Cells)
        {
            if (rowY is not null && cell.VirtY != rowY.Value)
            {
                continue;
            }
            var grh = layerIndex switch
            {
                0 => cell.G0,
                1 => cell.G1,
                2 => cell.G2,
                3 => cell.G3,
                _ => 0,
            };
            if (layerIndex == 0 && grh <= 0 && cell.G1 > 0)
            {
                grh = cell.G1;
            }
            if (grh <= 0)
            {
                continue;
            }
            if (layerIndex == 1 && cell.G0 <= 0 && cell.G1 > 0)
            {
                continue;
            }
            DrawPeekGrh(camera, cell.VirtX, cell.VirtY, grh, cell.Blocked, buffered, screenRect);
        }
    }

    private void DrawPeekObjects(WorldCamera camera, bool afterLayer2, Vector2 screen, int? rowY = null)
    {
        var screenRect = new Rect2(0, 0, screen.X, screen.Y);
        foreach (var cell in _peekCache.Cells)
        {
            if (rowY is not null && cell.VirtY != rowY.Value)
            {
                continue;
            }
            DrawPeekObject(camera, cell, afterLayer2, screenRect);
        }
    }

    private void DrawPeekObject(
        WorldCamera camera,
        WarpPeekTileCache.PeekCell cell,
        bool afterLayer2,
        Rect2 screenRect)
    {
        if (_resources?.Objects is null || cell.ObjectIndex <= 0)
        {
            return;
        }
        var obj = _resources.Objects.Get(cell.ObjectIndex);
        if (obj is null || obj.GrhIndex <= 0)
        {
            return;
        }
        if (afterLayer2)
        {
            if (!ShouldDrawObjectAfterLayer2(obj.ObjType))
            {
                return;
            }
        }
        else if (!ShouldDrawObjectAboveCharacter(obj.ObjType))
        {
            return;
        }
        var dest = TileDest(camera, cell.VirtX, cell.VirtY, obj.GrhIndex, buffered: true);
        if (dest is null || !screenRect.Intersects(dest.Value))
        {
            return;
        }
        if (IsVirtAnchorInPlayableMap(cell.VirtX, cell.VirtY))
        {
            return;
        }
        DrawGrh(obj.GrhIndex, dest.Value);
    }

    private void DrawPeekGrh(WorldCamera camera, int virtX, int virtY, int grh, byte blocked, bool buffered, Rect2 screenRect)
    {
        var dest = TileDest(camera, virtX, virtY, grh, buffered);
        if (dest is null || !screenRect.Intersects(dest.Value))
        {
            return;
        }
        if (IsVirtAnchorInPlayableMap(virtX, virtY))
        {
            return;
        }
        if (!DrawGrh(grh, dest.Value))
        {
            return;
        }
    }

    private static bool IsVirtAnchorInPlayableMap(int virtX, int virtY) =>
        virtX >= CsmMap.MinMapTile && virtX <= CsmMap.MaxMapTile
        && virtY >= CsmMap.MinMapTile && virtY <= CsmMap.MaxMapTile;

    private static Rect2 PlayableMapScreenRect(WorldCamera camera)
    {
        var left = camera.TileToScreenX(CsmMap.MinMapTile, buffered: true);
        var top = camera.TileToScreenY(CsmMap.MinMapTile, buffered: true);
        var w = (CsmMap.MaxMapTile - CsmMap.MinMapTile + 1) * CsmMap.TilePixels;
        var h = (CsmMap.MaxMapTile - CsmMap.MinMapTile + 1) * CsmMap.TilePixels;
        return new Rect2(left, top, w, h);
    }

    private static bool OverlapsPlayableMapScreen(WorldCamera camera, Rect2 dest) =>
        PlayableMapScreenRect(camera).Intersects(dest);

    private void DrawPeekDebugOverlay(Vector2 screen, IReadOnlyList<AdjacentMapRing.NeighborSlice> slices)
    {
        var camera = WorldCamera.Create(_session!, screen);
        var mapRect = PlayableMapScreenRect(camera);
        var seamColor = new Color(1f, 0.1f, 0.1f, 0.95f);
        var sliceColor = new Color(1f, 0.85f, 0.1f, 0.85f);
        var cellColor = new Color(0.2f, 1f, 0.35f, 0.75f);
        var rejectColor = new Color(1f, 0.2f, 0.9f, 0.6f);
        const float seamW = 2f;

        DrawRect(mapRect, seamColor, filled: false, width: seamW);

        var westSeamX = camera.TileToScreenX(CsmMap.MinMapTile, buffered: true);
        var eastSeamX = camera.TileToScreenX(CsmMap.MaxMapTile, buffered: true) + CsmMap.TilePixels;
        var northSeamY = camera.TileToScreenY(CsmMap.MinMapTile, buffered: true);
        var southSeamY = camera.TileToScreenY(CsmMap.MaxMapTile, buffered: true) + CsmMap.TilePixels;

        DrawLine(new Vector2(westSeamX, 0), new Vector2(westSeamX, screen.Y), seamColor, seamW);
        DrawLine(new Vector2(eastSeamX, 0), new Vector2(eastSeamX, screen.Y), seamColor, seamW);
        DrawLine(new Vector2(0, northSeamY), new Vector2(screen.X, northSeamY), seamColor, seamW);
        DrawLine(new Vector2(0, southSeamY), new Vector2(screen.X, southSeamY), seamColor, seamW);

        var cutColor = new Color(1f, 0.45f, 0.05f, 0.95f);
        foreach (var cut in _session!.WarpCuts)
        {
            if (cut.FaceDirX != 0)
            {
                var cutX = camera.TileToScreenX(cut.ExitX, buffered: true);
                if (cut.FaceDirX < 0)
                {
                    cutX += CsmMap.TilePixels;
                }
                DrawLine(new Vector2(cutX, 0), new Vector2(cutX, screen.Y), cutColor, seamW);
            }
            if (cut.FaceDirY != 0)
            {
                var cutY = camera.TileToScreenY(cut.ExitY, buffered: true);
                if (cut.FaceDirY < 0)
                {
                    cutY += CsmMap.TilePixels;
                }
                DrawLine(new Vector2(0, cutY), new Vector2(screen.X, cutY), cutColor, seamW);
            }
            var cutLabel = $"warp→{cut.DestMapId} exit({cut.ExitX},{cut.ExitY}) spawn({cut.SpawnX},{cut.SpawnY})";
            var labelPos = new Vector2(
                camera.TileToScreenX(cut.ExitX, buffered: true) + 2,
                camera.TileToScreenY(cut.ExitY, buffered: true) + 2);
            DrawDebugLabel(labelPos, cutLabel, cutColor);
        }

        foreach (var slice in slices)
        {
            var virtRect = VirtBoundsScreenRect(camera, slice.VirtMinX, slice.VirtMinY, slice.VirtMaxX, slice.VirtMaxY);
            DrawRect(virtRect, sliceColor, filled: false, width: 1.5f);
            var label = $"map{slice.Map.MapId} exit({slice.ExitX},{slice.ExitY}) spawn({slice.SpawnX},{slice.SpawnY}) face({slice.FaceDirX},{slice.FaceDirY})";
            DrawDebugLabel(virtRect.Position + new Vector2(2, 2), label, sliceColor);
        }

        if (PeekDiagnostics.GridEnabled)
        {
            var cells = _peekCache.Cells;
            foreach (var cell in cells)
            {
                var tileRect = VirtTileScreenRect(camera, cell.VirtX, cell.VirtY);
                DrawRect(tileRect, cellColor, filled: false);
            }

            foreach (var slice in slices)
            {
                for (var y = slice.SrcMinY; y <= slice.SrcMaxY; y++)
                {
                    for (var x = slice.SrcMinX; x <= slice.SrcMaxX; x++)
                    {
                        var virtX = slice.ExitX + (x - slice.SpawnX);
                        var virtY = slice.ExitY + (y - slice.SpawnY);
                        var tileRect = VirtTileScreenRect(camera, virtX, virtY);
                        if (!tileRect.Intersects(new Rect2(0, 0, screen.X, screen.Y)))
                        {
                            continue;
                        }
                        if (!WarpPeekTileCache.WouldDrawCell(virtX, virtY, slice)
                            || OverlapsPlayableMapScreen(camera, tileRect))
                        {
                            DrawRect(tileRect, rejectColor, filled: false);
                        }
                    }
                }
            }
        }

        var hudCells = _peekCache.Cells.Count;
        var hud = $"PEEK DEBUG | map {_session!.MapId} | slices {slices.Count} | cells {hudCells}";
        DrawDebugLabel(new Vector2(8, 8), hud, Colors.White);
        DrawDebugLabel(new Vector2(8, 24), "ROJO=grid 1..100 | NARANJA=corte warp | AMARILLO=slice (AO_PEEK_DEBUG_GRID=1 para celdas)", seamColor);
    }

    private static Rect2 VirtTileScreenRect(WorldCamera camera, int virtX, int virtY) =>
        new(
            camera.TileToScreenX(virtX, buffered: true),
            camera.TileToScreenY(virtY, buffered: true),
            CsmMap.TilePixels,
            CsmMap.TilePixels);

    private static Rect2 VirtBoundsScreenRect(WorldCamera camera, int virtMinX, int virtMinY, int virtMaxX, int virtMaxY)
    {
        var x = camera.TileToScreenX(virtMinX, buffered: true);
        var y = camera.TileToScreenY(virtMinY, buffered: true);
        var w = (virtMaxX - virtMinX + 1) * CsmMap.TilePixels;
        var h = (virtMaxY - virtMinY + 1) * CsmMap.TilePixels;
        return new Rect2(x, y, w, h);
    }

    private void DrawDebugLabel(Vector2 pos, string text, Color color)
    {
        var font = ThemeDB.FallbackFont;
        DrawString(font, pos, text, HorizontalAlignment.Left, -1, 11, color);
    }

    private CsmMap ActiveMap => _drawMap ?? _session!.Map!;

    private void DrawRoofLayer(WorldCamera camera, int minX, int maxX, int minY, int maxY, bool buffered)
    {
        var map = ActiveMap;
        for (var y = minY; y <= maxY; y++)
        {
            if (y < CsmMap.MinMapTile || y > CsmMap.MaxMapTile)
            {
                continue;
            }
            for (var x = minX; x <= maxX; x++)
            {
                if (x < CsmMap.MinMapTile || x > CsmMap.MaxMapTile)
                {
                    continue;
                }
                var grh = map.Tiles[x, y].Graphics[3];
                if (grh <= 0)
                {
                    continue;
                }
                Color? modulate = null;
                var roofTrigger = MapTrigger.NearRoof(map, x, y);
                if (roofTrigger != 0)
                {
                    var alpha = _session!.RoofFade.GetAlpha255(roofTrigger) / 255f;
                    modulate = new Color(1f, 1f, 1f, alpha);
                }
                DrawTileGrh(camera, x, y, grh, map.Tiles[x, y].Blocked != 0, buffered, modulate);
            }
        }
    }

    private void DrawTileLayer(WorldCamera camera, int layerIndex, int minX, int maxX, int minY, int maxY, bool buffered)
    {
        var map = ActiveMap;
        for (var y = minY; y <= maxY; y++)
        {
            if (y < CsmMap.MinMapTile || y > CsmMap.MaxMapTile)
            {
                continue;
            }
            for (var x = minX; x <= maxX; x++)
            {
                if (x < CsmMap.MinMapTile || x > CsmMap.MaxMapTile)
                {
                    continue;
                }
                var grh = map.Tiles[x, y].Graphics[layerIndex];
                if (grh <= 0)
                {
                    continue;
                }
                DrawTileGrh(camera, x, y, grh, map.Tiles[x, y].Blocked != 0, buffered);
            }
        }
    }

    private void DrawObjects(
        WorldCamera camera, int minX, int maxX, int minY, int maxY, bool afterLayer2)
    {
        if (_resources?.Objects is null)
        {
            return;
        }
        var map = ActiveMap;
        for (var y = minY; y <= maxY; y++)
        {
            if (y < CsmMap.MinMapTile || y > CsmMap.MaxMapTile)
            {
                continue;
            }
            for (var x = minX; x <= maxX; x++)
            {
                if (x < CsmMap.MinMapTile || x > CsmMap.MaxMapTile)
                {
                    continue;
                }
                var tile = map.Tiles[x, y];
                if (tile.ObjectIndex <= 0)
                {
                    continue;
                }
                if (tile.ObjectIsDroppedItem)
                {
                    DrawDroppedItem(camera, x, y);
                    continue;
                }
                var obj = _resources.Objects.Get(tile.ObjectIndex);
                if (obj is null || obj.GrhIndex <= 0)
                {
                    continue;
                }
                if (afterLayer2)
                {
                    if (!ShouldDrawObjectAfterLayer2(obj.ObjType))
                    {
                        continue;
                    }
                }
                else if (!ShouldDrawObjectAboveCharacter(obj.ObjType))
                {
                    continue;
                }
                var dest = TileDest(camera, x, y, obj.GrhIndex, buffered: true);
                if (dest is not null)
                {
                    DrawGrh(obj.GrhIndex, dest.Value);
                }
            }
        }
    }

    private void DrawTileGrh(WorldCamera camera, int mapX, int mapY, int grh, bool blocked, bool buffered, Color? modulate = null)
    {
        var dest = TileDest(camera, mapX, mapY, grh, buffered);
        if (dest is null)
        {
            if (!_loginPreview)
            {
                DrawRect(
                    new Rect2(camera.TileToScreenX(mapX, buffered), camera.TileToScreenY(mapY, buffered),
                        CsmMap.TilePixels, CsmMap.TilePixels),
                    TileFallback(grh, blocked));
            }
            return;
        }
        if (!DrawGrh(grh, dest.Value, modulate: modulate) && !_loginPreview)
        {
            DrawRect(dest.Value, TileFallback(grh, blocked));
        }
    }

    private void DrawDroppedItem(WorldCamera camera, int mapX, int mapY)
    {
        if (_resources?.Items is null || _drawMap is null)
        {
            return;
        }
        var tile = _drawMap.Tiles[mapX, mapY];
        if (!tile.ObjectIsDroppedItem || tile.ObjectIndex <= 0)
        {
            return;
        }
        var item = _resources.Items.Get(tile.ObjectIndex);
        var grh = item?.GrhIndex ?? 0;
        if (grh <= 0)
        {
            return;
        }
        var objType = item?.ObjType ?? 0;
        var dest = TileDest(camera, mapX, mapY, grh, buffered: true);
        if (dest is not null)
        {
            if (tile.ObjectElementalTags != 0)
            {
                DrawDroppedItemEnchantmentBack(grh, dest.Value, tile.ObjectElementalTags, objType);
            }
            DrawGrh(grh, dest.Value);
            if (tile.ObjectElementalTags != 0)
            {
                DrawDroppedItemEnchantmentFront(grh, dest.Value, tile.ObjectElementalTags, objType);
            }
        }
    }

    private void DrawDroppedItemEnchantmentBack(int grh, Rect2 dest, int elementalTags, int objType)
    {
        var ticks = (float)Time.GetTicksMsec();
        var pulse = 0.5f + 0.5f * Mathf.Sin(ticks / 240.0f);
        DrawReplicaAuraLayers(grh, dest, elementalTags, objType, ticks, pulse, 0.92f);
    }

    private void DrawDroppedItemEnchantmentFront(int grh, Rect2 dest, int elementalTags, int objType)
    {
        var ticks = (float)Time.GetTicksMsec();
        var pulse = 0.5f + 0.5f * Mathf.Sin(ticks / 240.0f);
        DrawReplicaAuraHighlight(grh, dest, elementalTags, objType, ticks, pulse, 0.84f);
        ItemAuraVisuals.DrawSparkles(this, dest, elementalTags, objType, pulse, ticks, 0.9f);
    }

    private static bool ShouldDrawObjectAfterLayer2(int objType) => objType switch
    {
        4 or 6 or 8 or 19 or 20 or 22 or 28 or 47 or 52 => true,
        _ => true,
    };

    private static bool ShouldDrawObjectAboveCharacter(int objType) => objType switch
    {
        4 or 6 or 8 or 20 or 22 or 27 or 28 or 47 => true,
        _ => false,
    };

    private void DrawCharacter(Vector2 screen, WorldCamera camera)
    {
        if (_session is null)
        {
            return;
        }
        ResolveSessionAppearance(out var body, out var head, out var weapon, out var shield, out var helmet);
        DrawCharacterBody(screen, camera, _session.TileX, _session.TileY, body, head,
            _session.Heading, _session.Motion, weapon, shield, _session.CharacterName,
            drawName: true, isNpc: false, minHp: 0, maxHp: 0, privilege: _session.Privilege,
            weaponTags: EquippedTagsFor(2));
        DrawCharacterHeadFor(screen, camera, _session.TileX, _session.TileY, body, head,
            _session.Heading, _session.Motion, _session.CharIndex, helmet);
    }

    private void ResolveSessionAppearance(out int body, out int head, out int weapon, out int shield, out int helmet)
    {
        DeathAppearance.Resolve(_session!.IsDead, _session.Body, _session.Head,
            _session.GfxWeapon, _session.GfxShield, _session.GfxHelmet,
            out body, out head, out weapon, out shield, out helmet);
    }

    private void DrawPeekCharacterBodiesAtRow(int rowY, Vector2 screen, WorldCamera camera)
    {
        var screenRect = new Rect2(0, 0, screen.X, screen.Y);
        foreach (var npc in _peekCache.Npcs)
        {
            if (PeekNpcMotion.CharacterSortRow(npc.VirtY) != rowY)
            {
                continue;
            }
            if (IsVirtAnchorInPlayableMap(npc.VirtX, npc.VirtY))
            {
                continue;
            }
            GetCharacterPixelPosition(camera, npc.VirtX, npc.VirtY, PeekNpcMotion, out var pixelX, out var pixelY);
            if (pixelX + CsmMap.TilePixels < 0 || pixelX > screenRect.Size.X
                || pixelY + CsmMap.TilePixels < 0 || pixelY > screenRect.Size.Y)
            {
                continue;
            }
            DrawCharacterBody(screen, camera, npc.VirtX, npc.VirtY, npc.Body, npc.Head, npc.Heading,
                PeekNpcMotion, npc.Weapon, npc.Shield, npc.Name,
                drawName: npc.ShowName && npc.Name.Length > 0, isNpc: true,
                minHp: npc.MinHp, maxHp: npc.MaxHp);
        }
    }

    private void DrawPeekCharacterHeadsAtRow(int rowY, Vector2 screen, WorldCamera camera)
    {
        var screenRect = new Rect2(0, 0, screen.X, screen.Y);
        foreach (var npc in _peekCache.Npcs)
        {
            if (PeekNpcMotion.CharacterSortRow(npc.VirtY) != rowY)
            {
                continue;
            }
            if (IsVirtAnchorInPlayableMap(npc.VirtX, npc.VirtY))
            {
                continue;
            }
            GetCharacterPixelPosition(camera, npc.VirtX, npc.VirtY, PeekNpcMotion, out var pixelX, out var pixelY);
            if (pixelX + CsmMap.TilePixels < 0 || pixelX > screenRect.Size.X
                || pixelY + CsmMap.TilePixels < 0 || pixelY > screenRect.Size.Y)
            {
                continue;
            }
            DrawCharacterHeadFor(screen, camera, npc.VirtX, npc.VirtY, npc.Body, npc.Head, npc.Heading, PeekNpcMotion, 0);
        }
    }

    private void DrawCharacterBodiesAtRow(int rowY, Vector2 screen, WorldCamera camera, int? selfTileX = null, int? selfTileY = null)
    {
        if (_session is null)
        {
            return;
        }
        var playerX = selfTileX ?? _session.TileX;
        var playerY = selfTileY ?? _session.TileY;
        if (_session.Motion.CharacterSortRow(playerY) == rowY)
        {
            ResolveSessionAppearance(out var body, out var head, out var weapon, out var shield, out _);
            DrawCharacterBody(screen, camera, playerX, playerY, body, head,
                _session.Heading, _session.Motion, weapon, shield, _session.CharacterName,
                drawName: true, isNpc: false, minHp: 0, maxHp: 0, privilege: _session.Privilege,
                weaponTags: EquippedTagsFor(2));
            DrawCharacterFx(camera, playerX, playerY, _session.Motion, body, _session.SelfFx);
        }
        foreach (var ch in _session.Characters.All)
        {
            if (ch.Invisible)
            {
                continue;
            }
            if (ch.Motion.CharacterSortRow(ch.TileY) == rowY)
            {
                DrawCharacterBody(screen, camera, ch.TileX, ch.TileY, ch.Body, ch.Head, ch.Heading, ch.Motion,
                    ch.Weapon, ch.Shield, ch.Name, drawName: ch.IsNpc && ch.Name.Length > 0,
                    isNpc: ch.IsNpc, minHp: ch.MinHp, maxHp: ch.MaxHp, privilege: ch.Privilege);
                DrawCharacterFx(camera, ch.TileX, ch.TileY, ch.Motion, ch.Body, ch.Fx);
            }
        }
    }

    private void DrawCharacterHeadsAtRow(int rowY, Vector2 screen, WorldCamera camera, int? selfTileX = null, int? selfTileY = null)
    {
        if (_session is null)
        {
            return;
        }
        var playerX = selfTileX ?? _session.TileX;
        var playerY = selfTileY ?? _session.TileY;
        if (_session.Motion.CharacterSortRow(playerY) == rowY)
        {
            ResolveSessionAppearance(out var body, out var head, out _, out _, out var helmet);
            DrawCharacterHeadFor(screen, camera, playerX, playerY, body, head,
                _session.Heading, _session.Motion, _session.CharIndex, helmet);
        }
        foreach (var ch in _session.Characters.All)
        {
            if (ch.Invisible)
            {
                continue;
            }
            if (ch.Motion.CharacterSortRow(ch.TileY) == rowY)
            {
                DrawCharacterHeadFor(screen, camera, ch.TileX, ch.TileY, ch.Body, ch.Head, ch.Heading, ch.Motion, ch.CharIndex, ch.Helmet);
            }
        }
    }

    private void GetCharacterPixelPosition(WorldCamera camera, int tileX, int tileY, CharacterMotion motion,
        out float pixelX, out float pixelY)
    {
        pixelX = camera.TileToScreenX(tileX, buffered: true) + motion.MoveOffsetX;
        pixelY = camera.TileToScreenY(tileY, buffered: true) + motion.MoveOffsetY;
    }

    private static void PruneCharacterFx(CharacterFx fx)
    {
        if (!fx.IsActive)
        {
            return;
        }
        if ((long)Time.GetTicksMsec() - fx.StartedMs > 2500)
        {
            fx.Clear();
        }
    }

    private void DrawCharacterFx(WorldCamera camera, int tileX, int tileY, CharacterMotion motion, int body, CharacterFx fx)
    {
        if (!fx.IsActive || _resources?.Fxs is null)
        {
            return;
        }
        var fxDef = _resources.Fxs.Get(fx.FxIndex);
        if (fxDef is null || fxDef.Animacion <= 0)
        {
            return;
        }
        GetCharacterPixelPosition(camera, tileX, tileY, motion, out var pixelX, out var pixelY);
        var bodyDef = _resources?.Bodies.Get(body);
        var offsetY = bodyDef?.BodyOffsetY ?? 0;
        var grhDef = ResolveGrh(fxDef.Animacion, (int)fx.StartedMs);
        if (grhDef is null)
        {
            return;
        }
        var dest = new Rect2(
            pixelX + fxDef.OffsetX,
            pixelY + fxDef.OffsetY + offsetY,
            grhDef.PixelWidth,
            grhDef.PixelHeight);
        DrawGrh(fxDef.Animacion, dest, animTick: (int)fx.StartedMs);
    }

    private void DrawCharacterBody(Vector2 screen, WorldCamera camera, int tileX, int tileY, int body, int head,
        int heading, CharacterMotion motion, int weapon, int shield, string name, bool drawName,
        bool isNpc, int minHp, int maxHp, int privilege = 0, int weaponTags = 0)
    {
        GetCharacterPixelPosition(camera, tileX, tileY, motion, out var pixelX, out var pixelY);
        var headingClamped = Math.Clamp(heading, 1, 4);
        var bodyDef = _resources?.Bodies.Get(body);
        var moving = motion.IsMoving;
        var bodyFrame = _resources?.Bodies.GetWalkFrame(body, headingClamped, moving, motion.WalkAnimTime);
        var headGrh = _resources?.Heads.GetGrh(head, headingClamped) ?? 0;

        if (bodyFrame is not { } slice || bodyDef is null)
        {
            if (drawName && tileX == _session?.TileX && tileY == _session?.TileY)
            {
                NetDiagnostics.Log("DRAW_FAIL", $"body={body} bodyDef={bodyDef is not null} frame={bodyFrame is not null} dead={_session?.IsDead}");
            }
            if (drawName)
            {
                DrawCharacterName(pixelX, pixelY, bodyDef, name, isNpc, isNpc && maxHp > 0, privilege);
            }
            return;
        }

        if (slice.UsesGrh)
        {
            var grhDef = ResolveGrh(slice.GrhIndex);
            if (grhDef is null)
            {
                if (drawName && tileX == _session?.TileX && tileY == _session?.TileY)
                {
                    NetDiagnostics.Log("DRAW_FAIL", $"casper grh={slice.GrhIndex} body={body} dead={_session?.IsDead}");
                }
                if (drawName)
                {
                    DrawCharacterName(pixelX, pixelY, bodyDef, name, isNpc, isNpc && maxHp > 0, privilege);
                }
                return;
            }
            var grhDest = CharacterSpriteLayout.BodyScreenRect(pixelX, pixelY, bodyDef, grhDef);
            var drew = DrawGrh(slice.GrhIndex, grhDest);
            if (!drew && !_loginPreview)
            {
                if (drawName && tileX == _session?.TileX && tileY == _session?.TileY)
                {
                    NetDiagnostics.Log("DRAW_FAIL", $"grh={slice.GrhIndex} file={grhDef.FileNum} w={grhDef.PixelWidth}");
                }
                DrawRect(grhDest.Grow(-2), new Color(0.75f, 0.85f, 1f, 0.55f));
            }
        }
        else
        {
            var bodyDest = CharacterSpriteLayout.BodyScreenRect(pixelX, pixelY, bodyDef, slice);
            if (!DrawSliceAt(slice, bodyDest) && !_loginPreview)
            {
                DrawRect(new Rect2(pixelX, pixelY, CsmMap.TilePixels, CsmMap.TilePixels).Grow(-4), new Color(0.85f, 0.45f, 0.2f));
            }
        }

        DrawWeaponAndShield(pixelX, pixelY, bodyDef, headingClamped, weapon, shield, motion, weaponTags);

        if (isNpc && maxHp > 0)
        {
            DrawNpcHealthBar(pixelX, pixelY, bodyDef, minHp, maxHp);
        }

        if (headGrh <= 0)
        {
            if (drawName)
            {
                DrawCharacterName(pixelX, pixelY, bodyDef, name, isNpc, isNpc && maxHp > 0, privilege);
            }
            return;
        }

        var headDef = ResolveGrh(headGrh);
        if (headDef is null)
        {
            if (drawName)
            {
                DrawCharacterName(pixelX, pixelY, bodyDef, name, isNpc, isNpc && maxHp > 0, privilege);
            }
            return;
        }

        if (drawName)
        {
            DrawCharacterName(pixelX, pixelY, bodyDef, name, isNpc, isNpc && maxHp > 0, privilege);
        }
    }

    /// <summary>VB6 engine.bas: nombre a los pies (BodyOffset + 30).</summary>
    private void DrawCharacterName(float pixelX, float pixelY, BodyDef? bodyDef, string name, bool isNpc, bool hasHpBar, int privilege = 0)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }
        var color = NameColor(isNpc, privilege);
        var ox = bodyDef?.BodyOffsetX ?? 0;
        var oy = bodyDef?.BodyOffsetY ?? 0;
        var offsetY = hasHpBar ? 8 : 0;
        const int fontSize = 11;
        if (privilege >= 5)
        {
            HudTextDraw.AtTopCenter(
                this,
                ThemeDB.FallbackFont,
                pixelX + ox + CsmMap.TilePixels * 0.5f,
                pixelY + oy + 31f + offsetY,
                name,
                fontSize,
                new Color(0.05f, 0.12f, 0.18f, 0.85f));
        }
        HudTextDraw.AtTopCenter(
            this,
            ThemeDB.FallbackFont,
            pixelX + ox + CsmMap.TilePixels * 0.5f,
            pixelY + oy + 30f + offsetY,
            name,
            fontSize,
            color);
    }

    private static Color NameColor(bool isNpc, int privilege)
    {
        if (privilege >= 5)
        {
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.GetTicksMsec() / 220.0f);
            return new Color(1f, 0.78f + 0.16f * pulse, 0.18f + 0.25f * pulse);
        }
        if (privilege > 0)
        {
            return new Color(0.55f, 0.82f, 1f);
        }
        return isNpc ? new Color(0.82f, 0.41f, 0.12f) : Colors.White;
    }

    private void DrawNpcHealthBar(float pixelX, float pixelY, BodyDef bodyDef, int minHp, int maxHp)
    {
        var ox = bodyDef.BodyOffsetX;
        var oy = bodyDef.BodyOffsetY;
        const float barW = 26f;
        const float barH = 4f;
        var barX = pixelX + ox + 5f;
        var barY = pixelY + oy + 37f;
        DrawRect(new Rect2(barX - 1f, barY - 1f, barW + 2f, barH + 2f), new Color(0.05f, 0.05f, 0.05f, 0.85f));
        var ratio = Math.Clamp(minHp / (float)maxHp, 0f, 1f);
        if (ratio > 0f)
        {
            DrawRect(new Rect2(barX, barY, barW * ratio, barH), new Color(0.95f, 0.1f, 0.1f));
        }
    }

    private void DrawWeaponAndShield(float pixelX, float pixelY, BodyDef body, int heading, int weapon, int shield,
        CharacterMotion motion, int weaponTags = 0)
    {
        var moving = motion.IsMoving;
        var nowMs = (int)Time.GetTicksMsec();
        var weaponGrh = _resources?.Weapons?.GetGrh(weapon, heading) ?? 0;
        var shieldGrh = _resources?.Shields?.GetGrh(shield, heading) ?? 0;
        motion.AdvanceWeaponAnim(nowMs, _resources?.Grhs, weaponGrh, shieldGrh);

        if (weaponGrh > 0)
        {
            var tick = motion.GetDirectionalAnimTick(weaponGrh, _resources?.Grhs, moving, nowMs);
            DrawDirectionalGrh(weaponGrh, pixelX, pixelY, body, tick, weaponTags);
        }
        if (shieldGrh > 0)
        {
            var tick = motion.GetDirectionalAnimTick(shieldGrh, _resources?.Grhs, moving, nowMs);
            DrawDirectionalGrh(shieldGrh, pixelX, pixelY, body, tick);
        }
    }

    private void DrawDirectionalGrh(int grhIndex, float pixelX, float pixelY, BodyDef body, int animTick = -1, int elementalTags = 0)
    {
        if (grhIndex <= 0)
        {
            return;
        }
        var def = ResolveGrh(grhIndex, animTick);
        if (def is null)
        {
            return;
        }
        var dest = CharacterSpriteLayout.HeadScreenRect(pixelX, pixelY, body, def);
        if (elementalTags != 0)
        {
            DrawEquippedItemEnchantmentBack(grhIndex, dest, elementalTags, animTick);
        }
        DrawGrh(grhIndex, dest, animTick);
        if (elementalTags != 0)
        {
            DrawEquippedItemEnchantmentFront(grhIndex, dest, elementalTags, animTick);
        }
    }

    private int EquippedTagsFor(int objType)
    {
        if (_session is null || _resources?.Items is null)
        {
            return 0;
        }
        for (var slot = 1; slot <= PlayerInventory.MaxSlots; slot++)
        {
            var inv = _session.Inventory.GetSlot(slot);
            if (inv.IsEmpty || !inv.Equipped || inv.ElementalTags == 0)
            {
                continue;
            }
            var item = _resources.Items.Get(inv.ObjIndex);
            if (item?.ObjType == objType)
            {
                return inv.ElementalTags;
            }
        }
        return 0;
    }

    private void DrawEquippedItemEnchantmentBack(int grh, Rect2 dest, int elementalTags, int animTick)
    {
        var ticks = (float)Time.GetTicksMsec();
        var pulse = 0.5f + 0.5f * Mathf.Sin(ticks / 180.0f);
        DrawReplicaAuraLayers(grh, dest, elementalTags, 2, ticks, pulse, 0.9f, animTick);
    }

    private void DrawEquippedItemEnchantmentFront(int grh, Rect2 dest, int elementalTags, int animTick)
    {
        var ticks = (float)Time.GetTicksMsec();
        var pulse = 0.5f + 0.5f * Mathf.Sin(ticks / 180.0f);
        DrawReplicaAuraHighlight(grh, dest, elementalTags, 2, ticks, pulse, 0.82f, animTick);
        ItemAuraVisuals.DrawSparkles(this, dest, elementalTags, 2, pulse, ticks, 0.95f);
    }

    private void DrawReplicaAuraLayers(int grh, Rect2 dest, int elementalTags, int objType, float ticks, float pulse, float strength, int animTick = -1)
    {
        foreach (var layer in ItemAuraVisuals.EnumerateReplicaLayers(elementalTags, objType, pulse, ticks, strength))
        {
            var grow = layer.Grow;
            var rect = new Rect2(dest.Position - new Vector2(grow, grow) + layer.Offset, dest.Size + new Vector2(grow * 2f, grow * 2f));
            DrawGrh(grh, rect, animTick, layer.Color);
        }
    }

    private void DrawReplicaAuraHighlight(int grh, Rect2 dest, int elementalTags, int objType, float ticks, float pulse, float strength, int animTick = -1)
    {
        foreach (var layer in ItemAuraVisuals.EnumerateHighlightLayers(elementalTags, objType, pulse, ticks, strength))
        {
            var rect = new Rect2(dest.Position + layer.Offset, dest.Size);
            DrawGrh(grh, rect, animTick, layer.Color);
        }
    }

    private void DrawCharacterHeadFor(Vector2 screen, WorldCamera camera, int tileX, int tileY, int body, int head,
        int heading, CharacterMotion motion, int charIndex, int helmet = 0)
    {
        GetCharacterPixelPosition(camera, tileX, tileY, motion, out var pixelX, out var pixelY);
        var headingClamped = Math.Clamp(heading, 1, 4);
        var bodyDef = _resources?.Bodies.Get(body);
        var headGrh = _resources?.Heads.GetGrh(head, headingClamped) ?? 0;
        if (bodyDef is null || headGrh <= 0)
        {
            return;
        }
        var headDef = ResolveGrh(headGrh);
        if (headDef is null)
        {
            return;
        }
        var headDest = CharacterSpriteLayout.HeadScreenRect(pixelX, pixelY, bodyDef, headDef);
        DrawGrh(headGrh, headDest);
        var helmetGrh = _resources?.Helmets?.GetGrh(helmet, headingClamped) ?? 0;
        if (helmetGrh > 0)
        {
            DrawDirectionalGrh(helmetGrh, pixelX, pixelY, bodyDef);
        }
        if (_session is not null && _session.Dialogs.TryGet(charIndex, out var bubble))
        {
            var font = AoUiFonts.Ui;
            CharacterDialogDraw.Draw(this, font, headDest.Position.X + headDest.Size.X * 0.5f, headDest.Position.Y, bubble);
        }
        if (_session is not null)
        {
            var fx = _session.FloatingTexts.GetActive((short)charIndex, Time.GetTicksMsec());
            if (fx.Count > 0)
            {
                CharacterFloatingTextDraw.DrawOverHead(
                    this, AoUiFonts.Ui,
                    headDest.Position.X + headDest.Size.X * 0.5f,
                    headDest.Position.Y,
                    fx);
            }
        }
    }

    private bool DrawSliceAt(SpriteSlice slice, Rect2 dest)
    {
        if (slice.UsesGrh)
        {
            return DrawGrh(slice.GrhIndex, dest);
        }
        var texture = _resources?.Textures.Get(slice.FileNum);
        if (texture is null)
        {
            return false;
        }
        var src = new Rect2(slice.SX, slice.SY, slice.Width, slice.Height);
        DrawTextureRectRegion(texture, dest, src);
        return true;
    }

    private Rect2? TileDest(WorldCamera camera, int mapX, int mapY, int grhIndex, bool buffered)
    {
        var def = ResolveGrh(grhIndex);
        if (def is null)
        {
            return null;
        }
        var x = camera.TileToScreenX(mapX, buffered);
        var y = camera.TileToScreenY(mapY, buffered);
        if (def.TileWidth != 1f)
        {
            x -= (int)(def.TileWidth * CsmMap.TilePixels / 2f) - CsmMap.TilePixels / 2f;
        }
        if (def.TileHeight != 1f)
        {
            y -= (int)(def.TileHeight * CsmMap.TilePixels) - CsmMap.TilePixels;
        }
        return new Rect2(x, y, def.PixelWidth, def.PixelHeight);
    }

    private GrhDef? ResolveGrh(int grhIndex, int animTick = -1)
    {
        if (_resources is null || grhIndex <= 0)
        {
            return null;
        }
        var tick = animTick >= 0 ? animTick : (int)Time.GetTicksMsec();
        return _resources.Grhs.ResolveDrawable(grhIndex, tick);
    }

    private bool DrawGrh(int grhIndex, Rect2 dest, int animTick = -1, Color? modulate = null)
    {
        var def = ResolveGrh(grhIndex, animTick);
        if (def is null || def.FileNum <= 0)
        {
            return false;
        }
        var texture = _resources?.Textures.Get(def.FileNum);
        if (texture is null)
        {
            return false;
        }
        var src = new Rect2(def.SX, def.SY, def.PixelWidth, def.PixelHeight);
        DrawTextureRectRegion(texture, dest, src, modulate ?? Colors.White);
        return true;
    }

    private static Color TileFallback(int graphic, bool blocked) =>
        blocked ? new Color(0.12f, 0.12f, 0.12f) : Color.FromHsv((graphic * 47) % 360 / 360f, 0.3f, 0.42f);
}
