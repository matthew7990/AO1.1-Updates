using System;
using Argentum.Client.Core;
using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.World;

/// <summary>Mapa destino precargado para transiciones seamless (salida de tile / ChangeMap).</summary>
public sealed class WarpPreviewState
{
    private CsmMap? _map;
    private int _pendingMapId;
    private int _resourceId;

    public int SpawnX { get; private set; } = 50;
    public int SpawnY { get; private set; } = 50;
    public int EntryHeading { get; private set; }
    public int ExitTileX { get; private set; }
    public int ExitTileY { get; private set; }

    public bool HasMap => _map is not null;

    public CsmMap? Map => _map;

    public static int MotionMarginTiles(CharacterMotion? motion)
    {
        if (motion is null)
        {
            return 6;
        }
        var extra = 6;
        if (motion.PendingCameraX != 0 || motion.PendingCameraY != 0)
        {
            extra += 8;
        }
        if (motion.IsMoving)
        {
            extra += 4;
        }
        extra += (int)Math.Ceiling(Math.Max(
            Math.Abs(motion.CameraOffsetX),
            Math.Abs(motion.CameraOffsetY)) / CsmMap.TilePixels);
        return extra;
    }

    public void StageExit(int mapId, int spawnX, int spawnY, int heading, int exitTileX, int exitTileY)
    {
        if (mapId <= 0)
        {
            return;
        }
        _pendingMapId = mapId;
        SpawnX = spawnX is >= 1 and <= CsmMap.PlayableSize ? spawnX : 50;
        SpawnY = spawnY is >= 1 and <= CsmMap.PlayableSize ? spawnY : 50;
        EntryHeading = heading;
        ExitTileX = exitTileX;
        ExitTileY = exitTileY;
        _map = MapLoader.TryLoad(mapId, mapId);
    }

    public void EnsureLoaded(int mapId, int resourceId)
    {
        if (mapId > 0)
        {
            _pendingMapId = mapId;
        }
        if (resourceId > 0)
        {
            _resourceId = resourceId;
        }
        if (_pendingMapId <= 0)
        {
            return;
        }
        _map = MapLoader.TryLoad(_pendingMapId, _resourceId > 0 ? _resourceId : _pendingMapId);
        if (SpawnX < 1 || SpawnX > CsmMap.PlayableSize)
        {
            SpawnX = 50;
        }
        if (SpawnY < 1 || SpawnY > CsmMap.PlayableSize)
        {
            SpawnY = 50;
        }
    }

    public void Clear()
    {
        _map = null;
        _pendingMapId = 0;
        _resourceId = 0;
        EntryHeading = 0;
        ExitTileX = 0;
        ExitTileY = 0;
    }
}
