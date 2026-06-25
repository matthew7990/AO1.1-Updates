using System;

namespace Argentum.Client.Core;

/// <summary>Resoluciones lógicas de render (como VB6 renderer fijo). Sin cálculo dinámico por frame.</summary>
public readonly struct RenderPreset
{
    public int Width { get; init; }
    public int Height { get; init; }
    public int HalfTilesX { get; init; }
    public int HalfTilesY { get; init; }
    public string Name { get; init; }

    public static RenderPreset FromPixels(string name, int width, int height) => new()
    {
        Name = name,
        Width = width,
        Height = height,
        HalfTilesX = Math.Max(1, (width / GameViewport.TilePixels) / 2),
        HalfTilesY = Math.Max(1, (height / GameViewport.TilePixels) / 2),
    };

    public static RenderPreset Hd720 => FromPixels("1280x720", 1280, 720);
    public static RenderPreset Hd1080 => FromPixels("1920x1080", 1920, 1080);
    public static RenderPreset Qhd1440 => FromPixels("2560x1440", 2560, 1440);

    public static RenderPreset Parse(string value)
    {
        var parts = value.Split('x', 'X');
        if (parts.Length == 2 &&
            int.TryParse(parts[0].Trim(), out var w) &&
            int.TryParse(parts[1].Trim(), out var h) &&
            w >= GameViewport.MinWidth && h >= GameViewport.MinHeight)
        {
            return FromPixels($"{w}x{h}", w, h);
        }
        return Hd1080;
    }
}
