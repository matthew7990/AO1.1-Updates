using System.Collections.Generic;
using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.World;

/// <summary>Celdas virtuales del peek precalculadas desde mapas adyacentes precargados.</summary>
public sealed class WarpPeekTileCache
{
    public readonly struct PeekCell
    {
        public int VirtX { get; init; }
        public int VirtY { get; init; }
        public int G0 { get; init; }
        public int G1 { get; init; }
        public int G2 { get; init; }
        public int G3 { get; init; }
        public int ObjectIndex { get; init; }
        public byte Blocked { get; init; }
    }

    public readonly struct PeekNpc
    {
        public int VirtX { get; init; }
        public int VirtY { get; init; }
        public int Body { get; init; }
        public int Head { get; init; }
        public int Heading { get; init; }
        public int Weapon { get; init; }
        public int Shield { get; init; }
        public int Helmet { get; init; }
        public string Name { get; init; }
        public bool ShowName { get; init; }
        public int MinHp { get; init; }
        public int MaxHp { get; init; }
        public bool FromServer { get; init; }
    }

    private readonly List<PeekCell> _cells = new();
    private readonly List<PeekNpc> _npcs = new();
    private readonly HashSet<int> _liveNpcMaps = new();
    private int _camMinX, _camMaxX, _camMinY, _camMaxY;
    private int _mapId, _tileX, _tileY;
    private ulong _sliceSignature;
    private int _liveVersion;

    public IReadOnlyList<PeekCell> Cells => _cells;
    public IReadOnlyList<PeekNpc> Npcs => _npcs;

    public bool IsCurrent(
        int mapId,
        int tileX,
        int tileY,
        WorldCamera camera,
        IReadOnlyList<AdjacentMapRing.NeighborSlice> slices,
        int liveVersion) =>
        mapId == _mapId
        && tileX == _tileX
        && tileY == _tileY
        && camera.MinBufferedX == _camMinX
        && camera.MaxBufferedX == _camMaxX
        && camera.MinBufferedY == _camMinY
        && camera.MaxBufferedY == _camMaxY
        && liveVersion == _liveVersion
        && ComputeSliceSignature(slices) == _sliceSignature;

    public void Rebuild(
        int mapId,
        int tileX,
        int tileY,
        WorldCamera camera,
        int fillMargin,
        IReadOnlyList<AdjacentMapRing.NeighborSlice> slices,
        NpcCatalog? npcCatalog,
        AdjacentPeekLiveState? liveState)
    {
        _cells.Clear();
        _npcs.Clear();
        _liveNpcMaps.Clear();
        _mapId = mapId;
        _tileX = tileX;
        _tileY = tileY;
        _camMinX = camera.MinBufferedX;
        _camMaxX = camera.MaxBufferedX;
        _camMinY = camera.MinBufferedY;
        _camMaxY = camera.MaxBufferedY;
        _sliceSignature = ComputeSliceSignature(slices);
        _liveVersion = liveState?.Version ?? 0;

        foreach (var slice in slices)
        {
            CollectSlice(camera, slice, fillMargin);
            if (liveState is not null)
            {
                CollectLiveNpcs(slice, liveState);
            }
            if (!_liveNpcMaps.Contains(slice.Map.MapId))
            {
                CollectStaticNpcs(slice, npcCatalog);
            }
        }
    }

    private void CollectLiveNpcs(AdjacentMapRing.NeighborSlice slice, AdjacentPeekLiveState liveState)
    {
        var hasAny = false;
        foreach (var live in liveState.ForMap(slice.Map.MapId))
        {
            var virtX = slice.ExitX + (live.SrcX - slice.SpawnX);
            var virtY = slice.ExitY + (live.SrcY - slice.SpawnY);
            if (!IsInPeekZone(virtX, virtY, slice))
            {
                continue;
            }

            hasAny = true;
            _npcs.Add(new PeekNpc
            {
                VirtX = virtX,
                VirtY = virtY,
                Body = live.Body,
                Head = live.Head,
                Heading = live.Heading,
                Weapon = live.Weapon,
                Shield = live.Shield,
                Helmet = live.Helmet,
                Name = live.Name,
                ShowName = live.Name.Length > 0,
                MinHp = live.MinHp,
                MaxHp = live.MaxHp,
                FromServer = true,
            });
        }
        if (hasAny)
        {
            _liveNpcMaps.Add(slice.Map.MapId);
        }
    }

    private void CollectStaticNpcs(AdjacentMapRing.NeighborSlice slice, NpcCatalog? npcCatalog)
    {
        if (npcCatalog is null)
        {
            return;
        }

        for (var y = slice.SrcMinY; y <= slice.SrcMaxY; y++)
        {
            for (var x = slice.SrcMinX; x <= slice.SrcMaxX; x++)
            {
                var virtX = slice.ExitX + (x - slice.SpawnX);
                var virtY = slice.ExitY + (y - slice.SpawnY);
                if (!IsInPeekZone(virtX, virtY, slice))
                {
                    continue;
                }

                var npcNumber = slice.Map.Tiles[x, y].NpcIndex;
                if (npcNumber <= 0)
                {
                    continue;
                }

                var def = npcCatalog.Get(npcNumber);
                if (def is null)
                {
                    continue;
                }

                _npcs.Add(new PeekNpc
                {
                    VirtX = virtX,
                    VirtY = virtY,
                    Body = def.Body,
                    Head = def.Head,
                    Heading = def.Heading,
                    Weapon = def.Weapon,
                    Shield = def.Shield,
                    Helmet = def.Helmet,
                    Name = def.Name,
                    ShowName = def.ShowName,
                    FromServer = false,
                });
            }
        }
    }

    private void CollectSlice(WorldCamera camera, AdjacentMapRing.NeighborSlice slice, int fillMargin)
    {
        for (var y = slice.SrcMinY; y <= slice.SrcMaxY; y++)
        {
            for (var x = slice.SrcMinX; x <= slice.SrcMaxX; x++)
            {
                var virtX = slice.ExitX + (x - slice.SpawnX);
                var virtY = slice.ExitY + (y - slice.SpawnY);
                if (!IsInPeekZone(virtX, virtY, slice))
                {
                    continue;
                }

                var tile = slice.Map.Tiles[x, y];
                _cells.Add(new PeekCell
                {
                    VirtX = virtX,
                    VirtY = virtY,
                    G0 = tile.Graphics[0],
                    G1 = tile.Graphics[1],
                    G2 = tile.Graphics[2],
                    G3 = tile.Graphics[3],
                    ObjectIndex = tile.ObjectIndex,
                    Blocked = tile.Blocked,
                });
            }
        }
    }

    private static bool IsInPeekZone(int virtX, int virtY, AdjacentMapRing.NeighborSlice slice)
    {
        if (virtX >= CsmMap.MinMapTile && virtX <= CsmMap.MaxMapTile
            && virtY >= CsmMap.MinMapTile && virtY <= CsmMap.MaxMapTile)
        {
            return false;
        }

        if (slice.FaceDirX < 0 && virtX >= CsmMap.MinMapTile)
        {
            return false;
        }
        if (slice.FaceDirX > 0 && virtX <= CsmMap.MaxMapTile)
        {
            return false;
        }
        if (slice.FaceDirY < 0 && virtY >= CsmMap.MinMapTile)
        {
            return false;
        }
        if (slice.FaceDirY > 0 && virtY <= CsmMap.MaxMapTile)
        {
            return false;
        }

        if (slice.FaceDirX < 0 && virtX >= slice.ExitX)
        {
            return false;
        }
        if (slice.FaceDirX > 0 && virtX <= slice.ExitX)
        {
            return false;
        }
        if (slice.FaceDirY < 0 && virtY >= slice.ExitY)
        {
            return false;
        }
        if (slice.FaceDirY > 0 && virtY <= slice.ExitY)
        {
            return false;
        }

        return virtX >= slice.VirtMinX && virtX <= slice.VirtMaxX
            && virtY >= slice.VirtMinY && virtY <= slice.VirtMaxY;
    }

    public static bool WouldDrawCell(int virtX, int virtY, AdjacentMapRing.NeighborSlice slice) =>
        IsInPeekZone(virtX, virtY, slice);

    private static ulong ComputeSliceSignature(IReadOnlyList<AdjacentMapRing.NeighborSlice> slices)
    {
        ulong sig = 0;
        foreach (var slice in slices)
        {
            sig = Hash(sig, slice.ExitX);
            sig = Hash(sig, slice.ExitY);
            sig = Hash(sig, slice.SpawnX);
            sig = Hash(sig, slice.SpawnY);
            sig = Hash(sig, slice.SrcMinX);
            sig = Hash(sig, slice.SrcMaxX);
            sig = Hash(sig, slice.SrcMinY);
            sig = Hash(sig, slice.SrcMaxY);
            sig = Hash(sig, slice.VirtMinX);
            sig = Hash(sig, slice.VirtMaxX);
            sig = Hash(sig, slice.VirtMinY);
            sig = Hash(sig, slice.VirtMaxY);
            sig = Hash(sig, slice.FaceDirX);
            sig = Hash(sig, slice.FaceDirY);
        }
        return sig;
    }

    private static ulong Hash(ulong seed, int value) =>
        seed * 31 + (uint)value;
}
