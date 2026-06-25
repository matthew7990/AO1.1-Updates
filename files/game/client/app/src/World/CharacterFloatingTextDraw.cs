using System.Collections.Generic;
using Argentum.Client.Ui;
using Godot;

namespace Argentum.Client.World;

public static class CharacterFloatingTextDraw
{
    public static void DrawOverHead(
        CanvasItem canvas,
        Font font,
        float headCenterX,
        float headTopY,
        IReadOnlyList<CharacterFloatingTexts.Fx> effects)
    {
        if (effects.Count == 0)
        {
            return;
        }
        var now = Time.GetTicksMsec();
        var yStack = 0f;
        for (var i = effects.Count - 1; i >= 0; i--)
        {
            var fx = effects[i];
            var alpha = fx.Alpha(now);
            if (alpha <= 0.01f)
            {
                continue;
            }
            var rise = fx.RiseOffset(now);
            var color = fx.Color;
            color.A *= alpha;
            var y = headTopY - 8f - rise - yStack;
            HudTextDraw.AtTopCenter(canvas, font, headCenterX, y, fx.Text, 12, color);
            yStack += 14f;
        }
    }
}
