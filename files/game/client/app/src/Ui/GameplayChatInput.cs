using System;
using Argentum.Client.Core;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>VB6 frmMain.SendTxt — abre con Enter (KeyUp) y envía al soltar Enter.</summary>
public partial class GameplayChatInput : LineEdit
{
    private static readonly Color Bg = new(0.04f, 0.05f, 0.08f, 0.92f);
    private static readonly Color Border = new(0.45f, 0.4f, 0.32f, 0.7f);

    public event Action<string>? MessageSubmitted;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        Visible = false;
        ZIndex = 15;
        PlaceholderText = "Escribí un mensaje…";
        MaxLength = 255;
        CaretBlink = true;
        ApplyLayout();
        GetViewport().SizeChanged += OnViewportResized;
    }

    private void OnViewportResized() => ApplyLayout();

    private void ApplyLayout()
    {
        var screen = GameViewport.GetRenderSize(GetViewport());
        const float margin = 12f;
        const float width = 420f;
        const float height = 28f;
        Position = new Vector2(margin, screen.Y - height - margin);
        Size = new Vector2(width, height);
    }

    public void Open()
    {
        ApplyLayout();
        Visible = true;
        Text = "";
        GrabFocus();
        QueueRedraw();
    }

    public void Close()
    {
        Visible = false;
        Text = "";
        ReleaseFocus();
    }

    public override void _Draw()
    {
        if (!Visible)
        {
            return;
        }
        var rect = new Rect2(Vector2.Zero, Size);
        DrawRect(rect, Bg);
        DrawRect(rect, Border, false, 1f);
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!Visible)
        {
            return;
        }
        if (@event is InputEventKey { Pressed: false, Echo: false, Keycode: Key.Escape })
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event is InputEventKey { Pressed: false, Echo: false } key
            && (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter))
        {
            var trimmed = Text.Trim();
            if (trimmed.Length > 0)
            {
                MessageSubmitted?.Invoke(trimmed);
            }
            Close();
            GetViewport().SetInputAsHandled();
        }
    }
}
