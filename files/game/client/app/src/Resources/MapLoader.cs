using System.Collections.Generic;
using System.IO;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Resources;

public static class MapLoader
{
    private static readonly Dictionary<int, CsmMap> FileCache = new();

    public static CsmMap? TryLoad(int mapId, int resourceId = 0)
    {
        var fileId = resourceId > 0 ? resourceId : mapId;
        if (!FileCache.TryGetValue(fileId, out var map))
        {
            map = LoadFromDisk(fileId);
            if (map is null)
            {
                return null;
            }
            FileCache[fileId] = map;
            ValidateKnownTiles(fileId, map);
        }
        MapDiagnostics.Log($"MapLoader mapId={mapId} file={fileId} name=\"{map.Name}\"");
        return map;
    }

    private static CsmMap? LoadFromDisk(int fileId)
    {
        var root = ResourcesRoot.Resolve();
        if (root is null)
        {
            return null;
        }
        var path = Path.Combine(root, "Mapas", $"mapa{fileId}.csm");
        if (!File.Exists(path))
        {
            GD.PushWarning($"Map file not found: {path}");
            return null;
        }
        return CsmMap.Load(path);
    }

    public static void ClearCache() => FileCache.Clear();

    private static void ValidateKnownTiles(int fileId, CsmMap map)
    {
        if (fileId == 1 && map.Tiles[50, 100].Graphics[0] != 6253)
        {
            GD.PushWarning($"CSM parse mismatch mapa1 (50,100): grh={map.Tiles[50, 100].Graphics[0]} expected 6253");
        }
        if (fileId == 2 && map.Tiles[50, 1].Graphics[0] != 6008)
        {
            GD.PushWarning($"CSM parse mismatch mapa2 (50,1): grh={map.Tiles[50, 1].Graphics[0]} expected 6008");
        }
    }
}
