using Argentum.Client.Resources;
using Argentum.Client.Ui;
using Godot;

namespace Argentum.Client.World;

/// <summary>VB6 Engine_Text_Render sobre la cabeza del personaje.</summary>
public static class CharacterDialogDraw
{
    private static readonly Color SpeakerColor = new("e8c878");
    private static readonly Color BubbleFill = new(0.04f, 0.05f, 0.08f, 0.82f);
    private static readonly Color BubbleBorder = new(0.45f, 0.4f, 0.32f, 0.75f);

    /// <param name="headTopY">Parte superior del sprite de cabeza en pantalla (HeadScreenRect.Position.Y).</param>
    public static void Draw(
        CanvasItem canvas,
        Font font,
        float headCenterX,
        float headTopY,
        in CharacterDialogs.Bubble bubble)
    {
        var now = Time.GetTicksMsec();
        var alpha = bubble.Alpha(now);
        if (alpha <= 0.01f)
        {
            return;
        }

        var rise = bubble.RiseOffset(now) * 0.35f;
        var speaker = bubble.Speaker;
        var message = bubble.Text;
        var speakerSize = string.IsNullOrEmpty(speaker)
            ? Vector2.Zero
            : font.GetStringSize(speaker, HorizontalAlignment.Left, -1, 10);
        var messageSize = font.GetStringSize(message, HorizontalAlignment.Left, -1, 10);
        var width = Mathf.Max(speakerSize.X, messageSize.X) + 12f;
        var height = (string.IsNullOrEmpty(speaker) ? 0f : 13f) + messageSize.Y + 8f;
        // VB6: texto justo encima de la cabeza (~4px), sin sumar HeadOffset otra vez.
        var bubbleBottom = headTopY - 4f - rise;
        var left = headCenterX - width * 0.5f;
        var rect = new Rect2(left, bubbleBottom - height, width, height);

        var fill = BubbleFill;
        fill.A = BubbleFill.A * alpha;
        var border = BubbleBorder;
        border.A = BubbleBorder.A * alpha;
        canvas.DrawRect(rect, fill);
        canvas.DrawRect(rect, border, false, 1f);

        var y = rect.Position.Y + 4f;
        if (!string.IsNullOrEmpty(speaker))
        {
            var nameColor = SpeakerColor;
            nameColor.A *= alpha;
            HudTextDraw.AtTopCenter(canvas, font, headCenterX, y, speaker, 10, nameColor);
            y += 13f;
        }

        var textColor = bubble.Color;
        textColor.A *= alpha;
        HudTextDraw.AtTopCenter(canvas, font, headCenterX, y, message, 10, textColor);
    }
}
