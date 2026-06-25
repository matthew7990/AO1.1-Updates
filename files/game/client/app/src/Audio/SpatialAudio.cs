using System;
using Argentum.Client.Core;
using Argentum.Client.World;

namespace Argentum.Client.Audio;

/// <summary>VB6 ao20audio.ComputeCharFxVolume / ComputeCharFxPan / EstaEnArea.</summary>
public static class SpatialAudio
{
    public static int TileDistance(int x1, int y1, int x2, int y2)
    {
        var dx = Math.Abs(x1 - x2);
        var dy = Math.Abs(y1 - y2);
        return Math.Max(dx, dy);
    }

    public static bool IsInAudibleArea(int srcX, int srcY, WorldSession listener)
    {
        if (srcX <= 0 || srcY <= 0)
        {
            return true;
        }
        var preset = GameViewport.Active;
        var borderX = preset.HalfTilesX + WorldCamera.TileBufferSizeX;
        var borderY = preset.HalfTilesY + WorldCamera.TileBufferSizeY;
        return srcX > listener.TileX - borderX
            && srcX < listener.TileX + borderX
            && srcY > listener.TileY - borderY
            && srcY < listener.TileY + borderY;
    }

    public static float VolumeDb(AoAudio.FxCategory category, int distance, float categoryBaseDb)
    {
        distance = Math.Abs(distance);
        if (distance >= 20)
        {
            return -80f;
        }
        var attenuation = distance * 1.2f;
        return categoryBaseDb - attenuation;
    }

    public static float Pan(int srcX, int listenerX, int distance)
    {
        if (distance == 0 || srcX == listenerX)
        {
            return 0f;
        }
        var sign = srcX < listenerX ? -1f : 1f;
        var scale = Math.Min(distance / 19f, 1f);
        return sign * scale * 0.85f;
    }
}
