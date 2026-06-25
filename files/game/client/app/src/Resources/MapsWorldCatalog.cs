using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Argentum.Client.World;

namespace Argentum.Client.Resources;

/// <summary>VB6 mapsworlddata.dat — grilla de mapas contiguos por mundo.</summary>
public sealed class MapsWorldCatalog
{
    private readonly List<WorldGrid> _worlds = [];

    private MapsWorldCatalog()
    {
    }

    public static MapsWorldCatalog? TryLoad(string root)
    {
        var path = Path.Combine(root, "init", "mapsworlddata.dat");
        if (!File.Exists(path))
        {
            return null;
        }
        var catalog = new MapsWorldCatalog();
        var sections = IniSections.Load(path);
        if (!sections.TryGetValue("INIT", out var init))
        {
            return null;
        }
        var total = ParseInt(init.GetValueOrDefault("TotalWorlds"));
        var maxWorld = total > 0 ? total : int.MaxValue;
        for (var w = 1; w <= maxWorld && sections.ContainsKey($"WORLDMAP{w}"); w++)
        {
            var key = $"WORLDMAP{w}";
            if (!sections.TryGetValue(key, out var section))
            {
                continue;
            }
            var width = ParseInt(section.GetValueOrDefault("Ancho"));
            var height = ParseInt(section.GetValueOrDefault("Alto"));
            if (width <= 0 || height <= 0)
            {
                continue;
            }
            var cells = new int[width * height];
            for (var i = 1; i <= cells.Length; i++)
            {
                cells[i - 1] = ParseInt(section.GetValueOrDefault(i.ToString(CultureInfo.InvariantCulture)));
            }
            catalog._worlds.Add(new WorldGrid(width, height, cells));
        }
        if (total <= 0 && catalog._worlds.Count == 0)
        {
            return null;
        }
        return catalog._worlds.Count > 0 ? catalog : null;
    }

    public int WorldCount => _worlds.Count;

    public bool TryGetAdjacent(int mapId, List<AdjacentMap> neighbors)
    {
        neighbors.Clear();
        foreach (var world in _worlds)
        {
            if (!world.TryFindIndex(mapId, out var index))
            {
                continue;
            }
            world.AddNeighbor(index, -1, 0, neighbors);  // west
            world.AddNeighbor(index, +1, 0, neighbors);  // east
            world.AddNeighbor(index, 0, -1, neighbors);  // north
            world.AddNeighbor(index, 0, +1, neighbors);  // south
            world.AddNeighbor(index, -1, -1, neighbors); // NW
            world.AddNeighbor(index, +1, -1, neighbors); // NE
            world.AddNeighbor(index, -1, +1, neighbors); // SW
            world.AddNeighbor(index, +1, +1, neighbors); // SE
            return neighbors.Count > 0;
        }
        return false;
    }

    public bool TryGetNeighbor(int mapId, int deltaGridX, int deltaGridY, out int neighborMapId)
    {
        neighborMapId = 0;
        foreach (var world in _worlds)
        {
            if (!world.TryFindIndex(mapId, out var index))
            {
                continue;
            }
            var col = (index - 1) % world.Width;
            var row = (index - 1) / world.Width;
            var ncol = col + deltaGridX;
            var nrow = row + deltaGridY;
            if (ncol < 0 || nrow < 0 || ncol >= world.Width || nrow >= world.Height)
            {
                return false;
            }
            var nextIndex = nrow * world.Width + ncol + 1;
            neighborMapId = world.MapAt(nextIndex);
            return neighborMapId > 0;
        }
        return false;
    }

    public bool TryBorderCross(int mapId, int x, int y, int heading, out int newMapId, out int newX, out int newY)
    {
        newMapId = 0;
        newX = 0;
        newY = 0;
        var dCol = 0;
        var dRow = 0;
        switch (heading)
        {
            case 2: // east
                if (x < CsmMap.PlayableSize)
                {
                    return false;
                }
                dCol = 1;
                break;
            case 4: // west
                if (x > 1)
                {
                    return false;
                }
                dCol = -1;
                break;
            case 3: // south
                if (y < CsmMap.PlayableSize)
                {
                    return false;
                }
                dRow = 1;
                break;
            case 1: // north
                if (y > 1)
                {
                    return false;
                }
                dRow = -1;
                break;
            default:
                return false;
        }
        foreach (var world in _worlds)
        {
            if (!world.TryFindIndex(mapId, out var index))
            {
                continue;
            }
            var col = (index - 1) % world.Width;
            var row = (index - 1) / world.Width;
            var ncol = col + dCol;
            var nrow = row + dRow;
            if (ncol < 0 || nrow < 0 || ncol >= world.Width || nrow >= world.Height)
            {
                return false;
            }
            var nextId = world.MapAt(nrow * world.Width + ncol + 1);
            if (nextId <= 0)
            {
                return false;
            }
            newMapId = nextId;
            (newX, newY) = heading switch
            {
                2 => (1, y),
                4 => (CsmMap.PlayableSize, y),
                3 => (x, 1),
                1 => (x, CsmMap.PlayableSize),
                _ => (x, y),
            };
            return true;
        }
        return false;
    }

    private static int ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

    private sealed class WorldGrid(int width, int height, int[] cells)
    {
        public int Width { get; } = width;
        public int Height { get; } = height;

        public bool TryFindIndex(int mapId, out int index)
        {
            for (var i = 0; i < cells.Length; i++)
            {
                if (cells[i] == mapId)
                {
                    index = i + 1;
                    return true;
                }
            }
            index = 0;
            return false;
        }

        public int MapAt(int oneBasedIndex)
        {
            if (oneBasedIndex <= 0 || oneBasedIndex > cells.Length)
            {
                return 0;
            }
            return cells[oneBasedIndex - 1];
        }

        public void AddNeighbor(int index, int dCol, int dRow, List<AdjacentMap> neighbors)
        {
            var col = (index - 1) % Width;
            var row = (index - 1) / Width;
            var ncol = col + dCol;
            var nrow = row + dRow;
            if (ncol < 0 || nrow < 0 || ncol >= Width || nrow >= Height)
            {
                return;
            }
            var mapId = MapAt(nrow * Width + ncol + 1);
            if (mapId <= 0)
            {
                return;
            }
            neighbors.Add(new AdjacentMap(
                mapId,
                dCol * CsmMap.PlayableSize,
                dRow * CsmMap.PlayableSize));
        }
    }
}

public readonly struct AdjacentMap(int mapId, int offsetTileX, int offsetTileY)
{
    public int MapId { get; } = mapId;
    public int OffsetTileX { get; } = offsetTileX;
    public int OffsetTileY { get; } = offsetTileY;
}
