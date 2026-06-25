using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.Ui;

internal static class InterfaceGrhDrawer
{
    public static void DrawAtDesign(
        CanvasItem canvas,
        GameResources resources,
        int grhIndex,
        float designX,
        float designY,
        AoUiScale scale)
    {
        var def = resources.Grhs.ResolveDrawable(grhIndex);
        if (def is null || def.FileNum <= 0)
        {
            return;
        }
        var texture = resources.Textures.Get(def.FileNum);
        if (texture is null)
        {
            return;
        }
        var dest = scale.MapRect(designX, designY, def.PixelWidth, def.PixelHeight);
        canvas.DrawTextureRectRegion(
            texture,
            dest,
            new Rect2(def.SX, def.SY, def.PixelWidth, def.PixelHeight));
    }

    public static void DrawFullscreen(CanvasItem canvas, GameResources resources, int grhIndex, AoUiScale scale)
    {
        var def = resources.Grhs.ResolveDrawable(grhIndex);
        if (def is null || def.FileNum <= 0)
        {
            return;
        }
        var texture = resources.Textures.Get(def.FileNum);
        if (texture is null)
        {
            return;
        }
        var dest = scale.MapRect(0, 0, AoUiScale.DesignWidth, AoUiScale.DesignHeight);
        canvas.DrawTextureRectRegion(
            texture,
            dest,
            new Rect2(def.SX, def.SY, def.PixelWidth, def.PixelHeight));
    }
}
