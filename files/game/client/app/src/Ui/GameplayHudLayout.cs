using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>Layout único del HUD — todas las posiciones salen de acá (viewport real).</summary>
public readonly struct GameplayHudLayout
{
    public const float Margin = 14f;
    public const float OrbMaxR = 54f;
    public const float OrbMinR = 20f;
    public const float OrbGap = 28f;
    public const float ExpBarH = 7f;
    public const float ExpBarW = 420f;
    public const float SlotSize = 40f;
    public const float SlotGap = 5f;
    public const int InvCols = 6;
    public const float BottomInset = 6f;

    public Vector2 Screen { get; init; }
    public float CenterX { get; init; }
    public float HotbarW { get; init; }
    public float HotbarX { get; init; }
    public float HotbarY { get; init; }
    public float ExpY { get; init; }
    public Vector2 LeftOrbCenter { get; init; }
    public Vector2 RightOrbCenter { get; init; }
    public Vector2 MapChipPos { get; init; }
    public float MapChipW { get; init; }
    /// <summary>Borde superior del texto caption (no baseline).</summary>
    public float CaptionTop { get; init; }
    public float StaminaTop { get; init; }
    /// <summary>Borde superior del % de EXP sobre la barra.</summary>
    public float ExpLabelTop { get; init; }

    public static GameplayHudLayout FromViewport(Vector2 screen, float mapChipW)
    {
        if (screen.X < 1f || screen.Y < 1f)
        {
            screen = new Vector2(1024f, 768f);
        }

        var centerX = screen.X * 0.5f;
        var hotbarW = SpellHotbar.SlotCount * (SlotSize + SlotGap) - SlotGap;
        var hotbarX = centerX - hotbarW * 0.5f;
        var hotbarY = screen.Y - BottomInset - SlotSize;
        var expY = hotbarY - ExpBarH - 6f;
        var orbY = hotbarY + SlotSize * 0.5f;

        return new GameplayHudLayout
        {
            Screen = screen,
            CenterX = centerX,
            HotbarW = hotbarW,
            HotbarX = hotbarX,
            HotbarY = hotbarY,
            ExpY = expY,
            LeftOrbCenter = new Vector2(hotbarX - OrbGap - OrbMaxR, orbY),
            RightOrbCenter = new Vector2(hotbarX + hotbarW + OrbGap + OrbMaxR, orbY),
            MapChipPos = new Vector2(screen.X - Margin - mapChipW, Margin),
            MapChipW = mapChipW,
            ExpLabelTop = expY - 14f,
            CaptionTop = hotbarY - 30f,
            StaminaTop = hotbarY - 44f,
        };
    }
}
