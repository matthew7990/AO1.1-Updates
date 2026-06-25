using Godot;

namespace Argentum.Client.Ui;

/// <summary>
/// Godot 4: DrawString usa la línea base (baseline), no la esquina superior.
/// Estos helpers alinean texto respecto a top/center/bottom reales.
/// </summary>
public static class HudTextDraw
{
    public static void AtTopLeft(CanvasItem canvas, Font font, Vector2 topLeft, string text, int fontSize, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        var pos = topLeft + new Vector2(0, font.GetAscent(fontSize));
        canvas.DrawString(font, pos, text, HorizontalAlignment.Left, -1, fontSize, color);
    }

    public static void AtTopCenter(CanvasItem canvas, Font font, float centerX, float topY, string text, int fontSize, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
        var pos = new Vector2(centerX - size.X * 0.5f, topY + font.GetAscent(fontSize));
        canvas.DrawString(font, pos, text, HorizontalAlignment.Left, -1, fontSize, color);
    }

    public static void AtTopRight(CanvasItem canvas, Font font, float rightX, float topY, string text, int fontSize, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
        var pos = new Vector2(rightX - size.X, topY + font.GetAscent(fontSize));
        canvas.DrawString(font, pos, text, HorizontalAlignment.Left, -1, fontSize, color);
    }

    public static void Centered(CanvasItem canvas, Font font, Vector2 center, string text, int fontSize, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
        var ascent = font.GetAscent(fontSize);
        var descent = font.GetDescent(fontSize);
        var height = ascent + descent;
        var pos = new Vector2(center.X - size.X * 0.5f, center.Y - height * 0.5f + ascent);
        canvas.DrawString(font, pos, text, HorizontalAlignment.Left, -1, fontSize, color);
    }

    public static void AtBottomRightInRect(CanvasItem canvas, Font font, Rect2 rect, string text, int fontSize, Color color, float pad = 3f)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
        var pos = new Vector2(
            rect.Position.X + rect.Size.X - size.X - pad,
            rect.Position.Y + rect.Size.Y - pad - font.GetDescent(fontSize));
        canvas.DrawString(font, pos, text, HorizontalAlignment.Left, -1, fontSize, color);
    }
}
