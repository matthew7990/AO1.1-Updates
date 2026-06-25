using Godot;

namespace Argentum.Client.Ui;

/// <summary>VB6 form coords: control Left/Top/Width/Height are in twips; ScaleMode Pixel → ÷15.</summary>
public static class Vb6FormCoords
{
    public const float TwipsPerPixel = 15f;

    public static Vector2 Position(int left, int top) =>
        new(left / TwipsPerPixel, top / TwipsPerPixel);

    public static Vector2 Size(int width, int height) =>
        new(width / TwipsPerPixel, height / TwipsPerPixel);

    public static Rect2 Rect(int left, int top, int width, int height) =>
        new(Position(left, top), Size(width, height));

    /// <summary>FrmLogear / frmNewAccount centered on frmConnect (1024×768).</summary>
    public static Rect2 CenteredOnConnect(int formWidth, int formHeight, int bottomMarginTwips = 450) =>
        new(
            (AoUiScale.DesignWidth - formWidth) / 2f,
            AoUiScale.DesignHeight - formHeight - bottomMarginTwips / TwipsPerPixel,
            formWidth,
            formHeight);
}
