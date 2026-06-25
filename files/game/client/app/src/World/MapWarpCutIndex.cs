using System;
using System.Collections.Generic;
using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.World;

/// <summary>
/// Cortes de mapa en tiles de salida reales (ExitMap), no en el borde 1..100 del grid.
/// Solo incluye salidas seamless (vecinos en mapsworlddata), no teleports internos.
/// </summary>
public sealed class MapWarpCutIndex
{
    private const int EdgeMarginTiles = 12;

    public readonly struct WarpCut
    {
        public int DestMapId { get; init; }
        public int ExitX { get; init; }
        public int ExitY { get; init; }
        public int SpawnX { get; init; }
        public int SpawnY { get; init; }
        public int FaceDirX { get; init; }
        public int FaceDirY { get; init; }
        public CsmMap? DestMap { get; init; }
    }

    private readonly List<WarpCut> _cuts = new();

    public IReadOnlyList<WarpCut> Cuts => _cuts;

    public static MapWarpCutIndex Build(CsmMap map, int mapId, MapsWorldCatalog? world)
    {
        var index = new MapWarpCutIndex();
        var best = new Dictionary<(int DirX, int DirY), (int EdgeDist, WarpCut Cut)>();

        for (var x = CsmMap.MinMapTile; x <= CsmMap.MaxMapTile; x++)
        {
            for (var y = CsmMap.MinMapTile; y <= CsmMap.MaxMapTile; y++)
            {
                var tile = map.Tiles[x, y];
                if (tile.ExitMap <= 0)
                {
                    continue;
                }

                var (dirX, dirY) = InferExitDirection(x, y);
                if (dirX == 0 && dirY == 0)
                {
                    continue;
                }

                if (!IsSeamlessExit(mapId, tile.ExitMap, x, y, tile.ExitX, tile.ExitY, dirX, dirY, world))
                {
                    continue;
                }

                var edgeDist = EdgeDistance(x, y, dirX, dirY);
                var key = (dirX, dirY);
                if (best.TryGetValue(key, out var existing) && edgeDist >= existing.EdgeDist)
                {
                    continue;
                }

                var destMap = MapLoader.TryLoad(tile.ExitMap, tile.ExitMap);
                best[key] = (edgeDist, new WarpCut
                {
                    DestMapId = tile.ExitMap,
                    ExitX = x,
                    ExitY = y,
                    SpawnX = tile.ExitX,
                    SpawnY = tile.ExitY,
                    FaceDirX = dirX,
                    FaceDirY = dirY,
                    DestMap = destMap,
                });
            }
        }

        foreach (var entry in best.Values)
        {
            index._cuts.Add(entry.Cut);
        }

        return index;
    }

    private static bool IsSeamlessExit(
        int mapId,
        int destMapId,
        int exitX,
        int exitY,
        int spawnX,
        int spawnY,
        int dirX,
        int dirY,
        MapsWorldCatalog? world)
    {
        if (world is not null
            && world.TryGetNeighbor(mapId, dirX, dirY, out var neighborId))
        {
            return neighborId == destMapId;
        }

        if (!IsNearMapEdge(exitX, exitY, dirX, dirY))
        {
            return false;
        }

        return HasOppositeSpawn(spawnX, spawnY, dirX, dirY);
    }

    private static bool IsNearMapEdge(int x, int y, int dirX, int dirY) =>
        EdgeDistance(x, y, dirX, dirY) <= EdgeMarginTiles;

    private static int EdgeDistance(int x, int y, int dirX, int dirY)
    {
        if (dirX < 0)
        {
            return x - CsmMap.MinMapTile;
        }
        if (dirX > 0)
        {
            return CsmMap.MaxMapTile - x;
        }
        if (dirY < 0)
        {
            return y - CsmMap.MinMapTile;
        }
        if (dirY > 0)
        {
            return CsmMap.MaxMapTile - y;
        }
        return int.MaxValue;
    }

    private static bool HasOppositeSpawn(int spawnX, int spawnY, int dirX, int dirY)
    {
        if (dirX < 0)
        {
            return spawnX >= CsmMap.MaxMapTile - EdgeMarginTiles;
        }
        if (dirX > 0)
        {
            return spawnX <= CsmMap.MinMapTile + EdgeMarginTiles;
        }
        if (dirY < 0)
        {
            return spawnY >= CsmMap.MaxMapTile - EdgeMarginTiles;
        }
        if (dirY > 0)
        {
            return spawnY <= CsmMap.MinMapTile + EdgeMarginTiles;
        }
        return false;
    }

    public void CollectPeekSlices(
        int tileX,
        int tileY,
        WorldCamera camera,
        Vector2 viewportPixels,
        int fillMargin,
        List<AdjacentMapRing.NeighborSlice> found)
    {
        found.Clear();
        if (_cuts.Count == 0)
        {
            return;
        }

        var maxBand = Math.Max(
            WorldCamera.TileBufferSizeX + WorldCamera.TileBufferSizeY,
            (int)Math.Ceiling(CsmMap.PlayableSize * AdjacentMapRing.SliceFraction));

        foreach (var cut in _cuts)
        {
            if (cut.DestMap is null || !ShouldIncludeCut(cut, tileX, tileY, camera))
            {
                continue;
            }
            if (!TryBuildSlice(cut, camera, viewportPixels, fillMargin, maxBand, out var slice))
            {
                continue;
            }
            found.Add(slice);
        }
    }

    private static bool ShouldIncludeCut(WarpCut cut, int tileX, int tileY, WorldCamera camera)
    {
        _ = tileX;
        _ = tileY;

        if (cut.FaceDirX < 0)
        {
            return camera.MinBufferedX <= CsmMap.MinMapTile
                && camera.MaxBufferedX >= cut.ExitX - 4;
        }
        if (cut.FaceDirX > 0)
        {
            return camera.MaxBufferedX >= CsmMap.MaxMapTile
                && camera.MinBufferedX <= cut.ExitX + 4;
        }
        if (cut.FaceDirY < 0)
        {
            return camera.MinBufferedY <= CsmMap.MinMapTile
                && camera.MaxBufferedY >= cut.ExitY - 4;
        }
        if (cut.FaceDirY > 0)
        {
            return camera.MaxBufferedY >= CsmMap.MaxMapTile
                && camera.MinBufferedY <= cut.ExitY + 4;
        }

        return false;
    }

    private static bool TryBuildSlice(
        WarpCut cut,
        WorldCamera camera,
        Vector2 viewportPixels,
        int fillMargin,
        int maxBand,
        out AdjacentMapRing.NeighborSlice slice)
    {
        slice = default;
        var gap = ComputeGapTiles(cut, camera, viewportPixels, fillMargin, maxBand);
        if (gap <= 0)
        {
            return false;
        }

        int srcMinX, srcMaxX, srcMinY, srcMaxY;
        int virtMinX, virtMaxX, virtMinY, virtMaxY;

        if (cut.FaceDirX < 0)
        {
            virtMaxX = CsmMap.MinMapTile - 1;
            virtMinX = CsmMap.MinMapTile - gap;
            srcMinX = cut.SpawnX + (virtMinX - cut.ExitX);
            srcMaxX = cut.SpawnX + (virtMaxX - cut.ExitX);
        }
        else if (cut.FaceDirX > 0)
        {
            virtMinX = CsmMap.MaxMapTile + 1;
            virtMaxX = CsmMap.MaxMapTile + gap;
            srcMinX = cut.SpawnX + (virtMinX - cut.ExitX);
            srcMaxX = cut.SpawnX + (virtMaxX - cut.ExitX);
        }
        else
        {
            srcMinX = CsmMap.MinMapTile;
            srcMaxX = CsmMap.MaxMapTile;
            virtMinX = CsmMap.MinMapTile;
            virtMaxX = CsmMap.MaxMapTile;
        }

        if (cut.FaceDirY < 0)
        {
            virtMaxY = CsmMap.MinMapTile - 1;
            virtMinY = CsmMap.MinMapTile - gap;
            srcMinY = cut.SpawnY + (virtMinY - cut.ExitY);
            srcMaxY = cut.SpawnY + (virtMaxY - cut.ExitY);
        }
        else if (cut.FaceDirY > 0)
        {
            virtMinY = CsmMap.MaxMapTile + 1;
            virtMaxY = CsmMap.MaxMapTile + gap;
            srcMinY = cut.SpawnY + (virtMinY - cut.ExitY);
            srcMaxY = cut.SpawnY + (virtMaxY - cut.ExitY);
        }
        else
        {
            srcMinY = CsmMap.MinMapTile;
            srcMaxY = CsmMap.MaxMapTile;
            virtMinY = CsmMap.MinMapTile;
            virtMaxY = CsmMap.MaxMapTile;
        }

        if (cut.FaceDirX != 0)
        {
            var camSrcMinY = cut.SpawnY + (camera.MinBufferedY - cut.ExitY);
            var camSrcMaxY = cut.SpawnY + (camera.MaxBufferedY - cut.ExitY);
            srcMinY = Math.Max(srcMinY, Math.Max(CsmMap.MinMapTile, camSrcMinY));
            srcMaxY = Math.Min(srcMaxY, Math.Min(CsmMap.MaxMapTile, camSrcMaxY));
        }
        if (cut.FaceDirY != 0)
        {
            var camSrcMinX = cut.SpawnX + (camera.MinBufferedX - cut.ExitX);
            var camSrcMaxX = cut.SpawnX + (camera.MaxBufferedX - cut.ExitX);
            srcMinX = Math.Max(srcMinX, Math.Max(CsmMap.MinMapTile, camSrcMinX));
            srcMaxX = Math.Min(srcMaxX, Math.Min(CsmMap.MaxMapTile, camSrcMaxX));
        }

        srcMinX = Math.Max(CsmMap.MinMapTile, srcMinX);
        srcMaxX = Math.Min(CsmMap.MaxMapTile, srcMaxX);
        srcMinY = Math.Max(CsmMap.MinMapTile, srcMinY);
        srcMaxY = Math.Min(CsmMap.MaxMapTile, srcMaxY);

        if (srcMinX > srcMaxX || srcMinY > srcMaxY)
        {
            return false;
        }

        virtMinX = cut.ExitX + (srcMinX - cut.SpawnX);
        virtMaxX = cut.ExitX + (srcMaxX - cut.SpawnX);
        virtMinY = cut.ExitY + (srcMinY - cut.SpawnY);
        virtMaxY = cut.ExitY + (srcMaxY - cut.SpawnY);

        slice = new AdjacentMapRing.NeighborSlice
        {
            Map = cut.DestMap!,
            ExitX = cut.ExitX,
            ExitY = cut.ExitY,
            SpawnX = cut.SpawnX,
            SpawnY = cut.SpawnY,
            SrcMinX = srcMinX,
            SrcMaxX = srcMaxX,
            SrcMinY = srcMinY,
            SrcMaxY = srcMaxY,
            VirtMinX = virtMinX,
            VirtMaxX = virtMaxX,
            VirtMinY = virtMinY,
            VirtMaxY = virtMaxY,
            FaceDirX = cut.FaceDirX,
            FaceDirY = cut.FaceDirY,
        };
        return true;
    }

    private static int ComputeGapTiles(
        WarpCut cut,
        WorldCamera camera,
        Vector2 viewportPixels,
        int fillMargin,
        int maxBand)
    {
        if (cut.FaceDirX < 0)
        {
            var blackTiles = Math.Max(0, (int)Math.Ceiling(camera.StartBufferedX / CsmMap.TilePixels));
            blackTiles = Math.Max(blackTiles, CsmMap.MinMapTile - camera.MinX);
            var band = Math.Max(blackTiles + fillMargin, WorldCamera.TileBufferSizeX);
            return Math.Clamp(band, 1, maxBand);
        }
        if (cut.FaceDirX > 0)
        {
            var mapEastPx = camera.TileToScreenX(CsmMap.MaxMapTile, buffered: true) + CsmMap.TilePixels;
            var blackTiles = Math.Max(0, (int)Math.Ceiling((viewportPixels.X - mapEastPx) / CsmMap.TilePixels));
            blackTiles = Math.Max(blackTiles, camera.MaxX - CsmMap.MaxMapTile);
            var band = Math.Max(blackTiles + fillMargin, WorldCamera.TileBufferSizeX);
            return Math.Clamp(band, 1, maxBand);
        }
        if (cut.FaceDirY < 0)
        {
            var blackTiles = Math.Max(0, (int)Math.Ceiling(camera.StartBufferedY / CsmMap.TilePixels));
            blackTiles = Math.Max(blackTiles, CsmMap.MinMapTile - camera.MinY);
            var band = Math.Max(blackTiles + fillMargin, WorldCamera.TileBufferSizeY);
            return Math.Clamp(band, 1, maxBand);
        }
        if (cut.FaceDirY > 0)
        {
            var mapSouthPx = camera.TileToScreenY(CsmMap.MaxMapTile, buffered: true) + CsmMap.TilePixels;
            var blackTiles = Math.Max(0, (int)Math.Ceiling((viewportPixels.Y - mapSouthPx) / CsmMap.TilePixels));
            blackTiles = Math.Max(blackTiles, camera.MaxY - CsmMap.MaxMapTile);
            var band = Math.Max(blackTiles + fillMargin, WorldCamera.TileBufferSizeY);
            return Math.Clamp(band, 1, maxBand);
        }
        return 1;
    }

    private static (int DirX, int DirY) InferExitDirection(int exitX, int exitY)
    {
        var toWest = exitX - CsmMap.MinMapTile;
        var toEast = CsmMap.MaxMapTile - exitX;
        var toNorth = exitY - CsmMap.MinMapTile;
        var toSouth = CsmMap.MaxMapTile - exitY;
        var min = Math.Min(Math.Min(toWest, toEast), Math.Min(toNorth, toSouth));
        if (min > EdgeMarginTiles)
        {
            return (0, 0);
        }
        if (min == toNorth)
        {
            return (0, -1);
        }
        if (min == toSouth)
        {
            return (0, 1);
        }
        if (min == toWest)
        {
            return (-1, 0);
        }
        return (1, 0);
    }
}
