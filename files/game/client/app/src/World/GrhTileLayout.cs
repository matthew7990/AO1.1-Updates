using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.World;

/// <summary>Posición en pantalla de un grh anclado a un tile (misma lógica que WorldView.TileDest).</summary>
public static class GrhTileLayout
{
    public static bool TryTileDest(
        GrhCatalog grhs,
        WorldCamera camera,
        int mapX,
        int mapY,
        int grhIndex,
        int tick,
        out Rect2 rect)
    {
        rect = default;
        var def = grhs.ResolveDrawable(grhIndex, tick);
        if (def is null || def.FileNum <= 0)
        {
            return false;
        }
        var x = camera.TileToScreenX(mapX, buffered: true);
        var y = camera.TileToScreenY(mapY, buffered: true);
        if (def.TileWidth != 1f)
        {
            x -= (int)(def.TileWidth * CsmMap.TilePixels / 2f) - CsmMap.TilePixels / 2f;
        }
        if (def.TileHeight != 1f)
        {
            y -= (int)(def.TileHeight * CsmMap.TilePixels) - CsmMap.TilePixels;
        }
        rect = new Rect2(x, y, def.PixelWidth, def.PixelHeight);
        return true;
    }
}
