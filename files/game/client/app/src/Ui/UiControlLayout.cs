using Godot;

namespace Argentum.Client.Ui;

internal static class UiControlLayout
{
    public static void PlaceAtDesign(this Control control, AoUiScale scale, float x, float y, float width, float height)
    {
        var rect = scale.MapRect(x, y, width, height);
        control.Position = rect.Position;
        control.Size = rect.Size;
        if (control is LineEdit)
        {
            control.AddThemeFontSizeOverride("font_size", scale.FontSize(11));
        }
    }
}
