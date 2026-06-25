using System;
using Argentum.Client.Core;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>Consola VB6 (RecTxt) + alertas arriba a la izquierda, fuera del HUD inferior.</summary>
public partial class GameplayConsole : Control
{
    private static readonly Color PanelFill = new(0.04f, 0.05f, 0.08f, 0.88f);
    private static readonly Color PanelBorder = new(0.45f, 0.4f, 0.32f, 0.55f);
    private static readonly Color AlertFill = new(0.18f, 0.12f, 0.06f, 0.94f);
    private static readonly Color AlertBorder = new(0.85f, 0.65f, 0.25f, 0.85f);
    private static readonly Color TextDefault = new("c8c0b4");
    private static readonly Color TextMuted = new(0.55f, 0.53f, 0.5f);

    private const float Margin = 12f;
    private const float PanelW = 400f;
    private const float ConsoleH = 148f;
    private const float AlertH = 34f;
    private const float LineH = 14f;

    private WorldSession? _world;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        GetViewport().SizeChanged += OnViewportResized;
    }

    private void OnViewportResized() => QueueRedraw();

    public void Bind(WorldSession? world)
    {
        _world = world;
        Visible = world is not null;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_world is null)
        {
            return;
        }

        var font = AoUiFonts.Ui;
        var x = Margin;
        var y = Margin;

        if (!string.IsNullOrWhiteSpace(_world.GameMessage))
        {
            DrawAlert(font, x, y, _world.GameMessage!);
            y += AlertH + 6f;
        }
        else if (_world.IsDead)
        {
            var death = string.IsNullOrWhiteSpace(_world.DeathMessage)
                ? "Estás muerto"
                : _world.DeathMessage!;
            DrawAlert(font, x, y, death, new Color("e04a4a"), AlertFill, new Color(0.5f, 0.15f, 0.15f, 0.9f));
            y += AlertH + 6f;
            HudTextDraw.AtTopLeft(this, font, new Vector2(x + 8f, y), "[R] resucitar cerca de un sacerdote", 10, TextMuted);
            y += 18f;
        }

        DrawConsolePanel(font, x, y);
    }

    private void DrawAlert(Font font, float x, float y, string text, Color? textColor = null,
        Color? fill = null, Color? border = null)
    {
        var panelW = PanelW;
        var rect = new Rect2(x, y, panelW, AlertH);
        DrawRect(rect, fill ?? AlertFill);
        DrawRect(rect, border ?? AlertBorder, false, 1.5f);
        HudTextDraw.AtTopLeft(this, font, new Vector2(x + 10f, y + 9f), text, 12, textColor ?? new Color("f0d878"));
    }

    private void DrawConsolePanel(Font font, float x, float y)
    {
        var rect = new Rect2(x, y, PanelW, ConsoleH);
        DrawRect(rect, PanelFill);
        DrawRect(rect, PanelBorder, false, 1f);
        HudTextDraw.AtTopLeft(this, font, new Vector2(x + 8f, y + 4f), "Consola", 9, TextMuted);

        var lines = _world!.Console.Lines;
        var maxVisible = (int)((ConsoleH - 22f) / LineH);
        var start = Math.Max(0, lines.Count - maxVisible);
        var ly = y + 20f;
        for (var i = start; i < lines.Count; i++)
        {
            var line = lines[i];
            var color = line.Color;
            if (color == default)
            {
                color = TextDefault;
            }
            HudTextDraw.AtTopLeft(this, font, new Vector2(x + 8f, ly), Truncate(line.Text, 52), 10, color);
            ly += LineH;
        }
    }

    private static string Truncate(string text, int maxChars)
    {
        if (text.Length <= maxChars)
        {
            return text;
        }
        return text[..(maxChars - 1)] + "…";
    }
}
