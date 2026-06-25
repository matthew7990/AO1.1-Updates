using System;
using Argentum.Client.Core;
using Godot;

namespace Argentum.Client.World;

/// <summary>
/// VB6 TileEngine_RenderScreen.RenderScreen — viewport en tiles del mapa activo (1..100).
/// </summary>
public readonly struct WorldCamera
{
    public const int TileBufferSizeX = 14;
    public const int TileBufferSizeY = 18;

    public int MinX { get; init; }
    public int MaxX { get; init; }
    public int MinY { get; init; }
    public int MaxY { get; init; }
    public int MinBufferedX { get; init; }
    public int MaxBufferedX { get; init; }
    public int MinBufferedY { get; init; }
    public int MaxBufferedY { get; init; }
    public float StartX { get; init; }
    public float StartY { get; init; }
    public float StartBufferedX { get; init; }
    public float StartBufferedY { get; init; }

    private static string? _lastCameraLogKey;

    public static WorldCamera Create(WorldSession session, Vector2? viewportPixels = null, int? centerTileX = null, int? centerTileY = null)
    {
        int halfX;
        int halfY;
        if (viewportPixels is { } viewport)
        {
            halfX = Math.Max(1, ((int)viewport.X / GameViewport.TilePixels) / 2);
            halfY = Math.Max(1, ((int)viewport.Y / GameViewport.TilePixels) / 2);
        }
        else
        {
            var preset = GameViewport.Active;
            halfX = preset.HalfTilesX;
            halfY = preset.HalfTilesY;
        }
        var motion = session.Motion;
        var tileX = centerTileX ?? session.TileX;
        var tileY = centerTileY ?? session.TileY;
        var centerX = motion.RenderCenterX(tileX);
        var centerY = motion.RenderCenterY(tileY);
        var pixelOffsetX = motion.CameraOffsetX;
        var pixelOffsetY = motion.CameraOffsetY;

        var minX = centerX - halfX;
        var maxX = centerX + halfX;
        var minY = centerY - halfY;
        var maxY = centerY + halfY;

        var minBufferedX = minX - TileBufferSizeX;
        var maxBufferedX = maxX + TileBufferSizeX;
        var minBufferedY = minY - 1;
        var maxBufferedY = maxY + TileBufferSizeY;

        var startX = pixelOffsetX - minX * CsmMap.TilePixels;
        var startY = pixelOffsetY - minY * CsmMap.TilePixels;
        var startBufferedX = -TileBufferSizeX * CsmMap.TilePixels + pixelOffsetX;
        var startBufferedY = pixelOffsetY - CsmMap.TilePixels;

        // VB6 extiende según dirección de scroll. Con camOff=0 al inicio del paso,
        // usar PendingCamera evita un frame con la extensión opuesta (micro-salto del piso).
        var canScrollX = centerX - halfX >= CsmMap.MinMapTile && centerX + halfX <= CsmMap.MaxMapTile;
        var canScrollY = centerY - halfY >= CsmMap.MinMapTile && centerY + halfY <= CsmMap.MaxMapTile;
        if (canScrollX)
        {
            var scrollWest = motion.PendingCameraX < 0
                || (motion.PendingCameraX == 0 && pixelOffsetX > 0f);
            if (scrollWest)
            {
                minX -= 1;
            }
            else
            {
                maxX += 10;
            }
        }
        if (canScrollY)
        {
            var scrollNorth = motion.PendingCameraY < 0
                || (motion.PendingCameraY == 0 && pixelOffsetY > 0f);
            if (scrollNorth)
            {
                minY -= 1;
            }
            else
            {
                maxY += 5;
            }
        }

        var clampedX = false;
        var clampedY = false;
        ClampHorizontal(
            ref minX, ref maxX, ref minBufferedX, ref maxBufferedX,
            ref startX, ref startBufferedX, pixelOffsetX, ref clampedX);
        ClampVertical(
            ref minY, ref maxY, ref minBufferedY, ref maxBufferedY,
            ref startY, ref startBufferedY, pixelOffsetY, ref clampedY);

        if (MovementDiagnostics.Enabled)
        {
            var key = $"{minX},{maxX},{minY},{maxY},{canScrollX},{canScrollY},{clampedX},{clampedY}";
            if (key != _lastCameraLogKey)
            {
                _lastCameraLogKey = key;
                MovementDiagnostics.Log("CAMERA",
                    $"center=({centerX},{centerY}) half=({halfX},{halfY}) " +
                    $"pCam=({motion.PendingCameraX},{motion.PendingCameraY}) " +
                    $"scroll=({canScrollX},{canScrollY}) clamp=({clampedX},{clampedY}) " +
                    $"range=({minX}-{maxX},{minY}-{maxY}) off=({pixelOffsetX:F1},{pixelOffsetY:F1})");
            }
        }

        return new WorldCamera
        {
            MinX = minX,
            MaxX = maxX,
            MinY = minY,
            MaxY = maxY,
            MinBufferedX = minBufferedX,
            MaxBufferedX = maxBufferedX,
            MinBufferedY = minBufferedY,
            MaxBufferedY = maxBufferedY,
            StartX = startX,
            StartY = startY,
            StartBufferedX = startBufferedX,
            StartBufferedY = startBufferedY,
        };
    }

    public float TileToScreenX(int mapX, bool buffered) =>
        buffered
            ? StartBufferedX + (mapX - MinBufferedX) * CsmMap.TilePixels
            : StartX + mapX * CsmMap.TilePixels;

    public float TileToScreenY(int mapY, bool buffered) =>
        buffered
            ? StartBufferedY + (mapY - MinBufferedY) * CsmMap.TilePixels
            : StartY + mapY * CsmMap.TilePixels;

    public bool TryScreenToTile(Vector2 screen, out int tileX, out int tileY)
    {
        var relX = screen.X - StartBufferedX;
        var relY = screen.Y - StartBufferedY;
        if (relX < 0 || relY < 0)
        {
            tileX = 0;
            tileY = 0;
            return false;
        }
        tileX = MinBufferedX + (int)(relX / CsmMap.TilePixels);
        tileY = MinBufferedY + (int)(relY / CsmMap.TilePixels);
        return tileX >= CsmMap.MinMapTile && tileX <= CsmMap.MaxMapTile
               && tileY >= CsmMap.MinMapTile && tileY <= CsmMap.MaxMapTile;
    }

    private static void ClampHorizontal(
        ref int minX, ref int maxX, ref int minBufferedX, ref int maxBufferedX,
        ref float startX, ref float startBufferedX, float pixelOffsetX, ref bool clamped)
    {
        if (minX < CsmMap.MinMapTile)
        {
            clamped = true;
            startBufferedX = pixelOffsetX - minX * CsmMap.TilePixels;
            maxX -= minX;
            maxBufferedX -= minX;
            minX = CsmMap.MinMapTile;
            minBufferedX = CsmMap.MinMapTile;
        }
        else if (minBufferedX < CsmMap.MinMapTile)
        {
            clamped = true;
            startBufferedX -= (minBufferedX - CsmMap.MinMapTile) * CsmMap.TilePixels;
            minBufferedX = CsmMap.MinMapTile;
        }
        else if (maxX > CsmMap.MaxMapTile)
        {
            clamped = true;
            maxX = CsmMap.MaxMapTile;
            maxBufferedX = CsmMap.MaxMapTile;
        }
        else if (maxBufferedX > CsmMap.MaxMapTile)
        {
            clamped = true;
            maxBufferedX = CsmMap.MaxMapTile;
        }
    }

    private static void ClampVertical(
        ref int minY, ref int maxY, ref int minBufferedY, ref int maxBufferedY,
        ref float startY, ref float startBufferedY, float pixelOffsetY, ref bool clamped)
    {
        if (minY < CsmMap.MinMapTile)
        {
            clamped = true;
            startBufferedY = pixelOffsetY - minY * CsmMap.TilePixels;
            maxY -= minY;
            maxBufferedY -= minY;
            minY = CsmMap.MinMapTile;
            minBufferedY = CsmMap.MinMapTile;
        }
        else if (minBufferedY < CsmMap.MinMapTile)
        {
            clamped = true;
            startBufferedY -= (minBufferedY - CsmMap.MinMapTile) * CsmMap.TilePixels;
            minBufferedY = CsmMap.MinMapTile;
        }
        else if (maxY > CsmMap.MaxMapTile)
        {
            clamped = true;
            maxY = CsmMap.MaxMapTile;
            maxBufferedY = CsmMap.MaxMapTile;
        }
        else if (maxBufferedY > CsmMap.MaxMapTile)
        {
            clamped = true;
            maxBufferedY = CsmMap.MaxMapTile;
        }
    }

    public readonly struct TileRect
    {
        public int MinX { get; init; }
        public int MinY { get; init; }
        public int MaxX { get; init; }
        public int MaxY { get; init; }
    }
}
