using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.World;

/// <summary>VB6 Char_Render + PresentComposedTexture (256×256, TextureY = height - 32).</summary>
internal static class CharacterSpriteLayout
{
    private const int ComposedWidth = 256;
    private const int ComposedHeight = 256;
    private const int TextureX = ComposedWidth / 2 - 16;
    private const int TextureY = ComposedHeight - 32;

    public static Rect2 BodyScreenRect(float pixelX, float pixelY, BodyDef body, SpriteSlice slice)
    {
        var presentX = pixelX + body.BodyOffsetX;
        var presentY = pixelY + body.BodyOffsetY;
        var (cx, cy) = BreathingDrawPos(TextureX, TextureY, slice.Width, slice.Height);
        return new Rect2(
            ComposedToScreenX(presentX, cx),
            ComposedToScreenY(presentY, cy),
            slice.Width,
            slice.Height);
    }

    public static Rect2 BodyScreenRect(float pixelX, float pixelY, BodyDef body, GrhDef grhDef) =>
        BodyScreenRect(pixelX, pixelY, body, grhDef.PixelWidth, grhDef.PixelHeight);

    private static Rect2 BodyScreenRect(float pixelX, float pixelY, BodyDef body, int width, int height)
    {
        var presentX = pixelX + body.BodyOffsetX;
        var presentY = pixelY + body.BodyOffsetY;
        var (cx, cy) = BreathingDrawPos(TextureX, TextureY, width, height);
        return new Rect2(
            ComposedToScreenX(presentX, cx),
            ComposedToScreenY(presentY, cy),
            width,
            height);
    }

    public static Rect2 HeadScreenRect(float pixelX, float pixelY, BodyDef body, GrhDef headDef)
    {
        var presentX = pixelX + body.BodyOffsetX;
        var presentY = pixelY + body.BodyOffsetY;
        // VB6: OffHead = HeadOffset.y - 1 - BodyOffset.y (HeadOffset includes BodyOffset at load).
        var offHead = body.HeadOffsetY - 1 - body.BodyOffsetY;
        var anchorX = TextureX + body.HeadOffsetX;
        var anchorY = TextureY + offHead;
        var (dx, dy) = GrhDrawPos(anchorX, anchorY, headDef);
        return new Rect2(
            ComposedToScreenX(presentX, dx),
            ComposedToScreenY(presentY, dy),
            headDef.PixelWidth,
            headDef.PixelHeight);
    }

    private static float ComposedToScreenX(float presentX, float composedX) =>
        presentX - ComposedWidth / 2f + 16 + composedX;

    private static float ComposedToScreenY(float presentY, float composedY) =>
        presentY - ComposedHeight + 32 + composedY;

    /// <summary>VB6 Draw_Grh_Breathing(center=1).</summary>
    private static (float X, float Y) BreathingDrawPos(int anchorX, int anchorY, int width, int height)
    {
        var x = (float)anchorX;
        var y = (float)anchorY;
        var tileWidth = width / (float)CsmMap.TilePixels;
        var tileHeight = height / (float)CsmMap.TilePixels;
        if (tileWidth != 1f)
        {
            x += (CsmMap.TilePixels - width) / 2f;
        }
        if (tileHeight != 1f)
        {
            y -= (int)(tileHeight * CsmMap.TilePixels) - CsmMap.TilePixels;
        }
        return (x, y);
    }

    /// <summary>VB6 Draw_Grh(center=1).</summary>
    private static (float X, float Y) GrhDrawPos(int anchorX, int anchorY, GrhDef def)
    {
        var x = (float)anchorX;
        var y = (float)anchorY;
        if (def.TileWidth != 1f)
        {
            x -= (int)(def.TileWidth * CsmMap.TilePixels / 2f) - CsmMap.TilePixels / 2f;
        }
        if (def.TileHeight != 1f)
        {
            y -= (int)(def.TileHeight * CsmMap.TilePixels) - CsmMap.TilePixels;
        }
        return (x, y);
    }
}
