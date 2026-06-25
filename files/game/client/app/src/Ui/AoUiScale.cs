using System;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>
/// Canvas VB6 1024×768 centrado en el viewport. Escala hacia abajo si la ventana es chica;
/// nunca amplía bitmaps por encima de 1:1 (letterbox en pantallas grandes).
/// </summary>
public readonly struct AoUiScale
{
    public const float DesignWidth = 1024f;
    public const float DesignHeight = 768f;
    public const float MaxUniform = 1f;

    public Vector2 Viewport { get; }
    public float Uniform { get; }
    public Vector2 Scale { get; }
    public Vector2 Offset { get; }

    public int FontSize(int designSize) => Math.Max(8, (int)MathF.Round(designSize * Uniform));

    public Rect2 DesignCanvas => MapRect(0, 0, DesignWidth, DesignHeight);

    public AoUiScale(Vector2 viewport)
    {
        Viewport = viewport;
        var fit = MathF.Min(viewport.X / DesignWidth, viewport.Y / DesignHeight);
        Uniform = MathF.Min(MaxUniform, fit);
        if (Uniform <= 0f)
        {
            Uniform = MaxUniform;
        }
        Scale = new Vector2(Uniform, Uniform);
        Offset = new Vector2(
            (viewport.X - DesignWidth * Uniform) * 0.5f,
            (viewport.Y - DesignHeight * Uniform) * 0.5f);
    }

    public static AoUiScale Current(Viewport viewport) =>
        new(viewport.GetVisibleRect().Size);

    public Rect2 MapRect(float x, float y, float width, float height) =>
        new(Offset.X + x * Uniform, Offset.Y + y * Uniform, width * Uniform, height * Uniform);

    public Vector2 MapPoint(float x, float y) =>
        Offset + new Vector2(x * Uniform, y * Uniform);

    public Vector2 MapSize(float width, float height) =>
        new(width * Uniform, height * Uniform);

    public bool Hit(Rect2 designRect, Vector2 screenPoint) =>
        MapRect(designRect.Position.X, designRect.Position.Y, designRect.Size.X, designRect.Size.Y)
            .HasPoint(screenPoint);

    public Vector2 ScreenToDesign(Vector2 screenPoint) =>
        new((screenPoint.X - Offset.X) / Uniform, (screenPoint.Y - Offset.Y) / Uniform);

    public void DrawLetterbox(CanvasItem canvas, Color color)
    {
        var top = DesignCanvas.Position.Y;
        if (top > 0f)
        {
            canvas.DrawRect(new Rect2(0, 0, Viewport.X, top), color);
        }
        var bottom = DesignCanvas.Position.Y + DesignCanvas.Size.Y;
        if (bottom < Viewport.Y)
        {
            canvas.DrawRect(new Rect2(0, bottom, Viewport.X, Viewport.Y - bottom), color);
        }
        var left = DesignCanvas.Position.X;
        if (left > 0f)
        {
            canvas.DrawRect(new Rect2(0, DesignCanvas.Position.Y, left, DesignCanvas.Size.Y), color);
        }
        var right = DesignCanvas.Position.X + DesignCanvas.Size.X;
        if (right < Viewport.X)
        {
            canvas.DrawRect(new Rect2(right, DesignCanvas.Position.Y, Viewport.X - right, DesignCanvas.Size.Y), color);
        }
    }
}
