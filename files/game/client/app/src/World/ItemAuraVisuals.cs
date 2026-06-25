using System;
using System.Collections.Generic;
using Godot;

namespace Argentum.Client.World;

public static class ItemAuraVisuals
{
    public readonly record struct GlowLayer(Color Color, float Grow, Vector2 Offset);

    private static Texture2D? _lightningSheet;
    private static Texture2D? _bloodSheet;
    private static Texture2D? _flameParticleSheet;
    private static Texture2D? _flameBurstSheet;

    public static IEnumerable<GlowLayer> EnumerateGlowLayers(int elementalTags, int objType, float pulse, float ticks, float strength = 1f)
    {
        if (ItemAffixes.ElementMask(elementalTags) == 0)
        {
            yield break;
        }

        var color = PrimaryColor(elementalTags);
        var grow = objType == 2 ? 2.8f : 1.9f;
        var drift = objType == 2 ? 1.2f : 0.7f;
        yield return new GlowLayer(WithAlpha(color, (0.18f + pulse * 0.08f) * strength), grow * strength, Vector2.Zero);
        yield return new GlowLayer(WithAlpha(Colors.White, (0.08f + pulse * 0.05f) * strength), grow * 0.55f * strength,
            new Vector2(Mathf.Sin(ticks / 80f) * drift, Mathf.Cos(ticks / 110f) * drift * 0.6f));
    }

    public static void DrawBackdrop(CanvasItem canvas, Rect2 dest, int elementalTags, int objType, float pulse, float ticks, float strength = 1f)
    {
        if (ItemAffixes.ElementMask(elementalTags) == 0)
        {
            return;
        }

        var scaledStrength = ScaledStrength(objType, strength);
        switch (ThemeFor(elementalTags))
        {
            case AuraTheme.Fire:
                DrawFireBackdrop(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
            case AuraTheme.Lightning:
                DrawLightningBackdrop(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
            case AuraTheme.Blood:
                DrawBloodBackdrop(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
            case AuraTheme.Holy:
                DrawHolyBackdrop(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
            case AuraTheme.Frost:
                DrawFrostBackdrop(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
            case AuraTheme.Earth:
                DrawEarthBackdrop(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
            case AuraTheme.Shadow:
                DrawShadowBackdrop(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
        }
    }

    public static void DrawOverlay(CanvasItem canvas, Rect2 dest, int elementalTags, int objType, float pulse, float ticks, float strength = 1f)
    {
        if (ItemAffixes.ElementMask(elementalTags) == 0)
        {
            return;
        }

        var scaledStrength = ScaledStrength(objType, strength);
        switch (ThemeFor(elementalTags))
        {
            case AuraTheme.Fire:
                DrawFireFront(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
            case AuraTheme.Lightning:
                DrawLightningFront(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
            case AuraTheme.Blood:
                DrawBloodFront(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
            case AuraTheme.Holy:
                DrawHolyFront(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
            case AuraTheme.Frost:
                DrawFrostFront(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
            case AuraTheme.Earth:
                DrawEarthFront(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
            case AuraTheme.Shadow:
                DrawShadowFront(canvas, dest, pulse, ticks, objType, scaledStrength);
                break;
        }
    }

    public static Color PrimaryColor(int elementalTags)
    {
        return ThemeFor(elementalTags) switch
        {
            AuraTheme.Fire => new Color("ff8d42"),
            AuraTheme.Lightning => new Color("84d0ff"),
            AuraTheme.Blood => new Color("c92b35"),
            AuraTheme.Holy => new Color("fff0a8"),
            AuraTheme.Frost => new Color("9fe8ff"),
            AuraTheme.Earth => new Color("c9a06c"),
            AuraTheme.Shadow => new Color("8d72d9"),
            _ => new Color("d9c07a"),
        };
    }

    public static Color AccentColor(int elementalTags)
    {
        return ThemeFor(elementalTags) switch
        {
            AuraTheme.Fire => new Color("ffcf6a"),
            AuraTheme.Lightning => new Color("f4fbff"),
            AuraTheme.Blood => new Color("6d0b12"),
            AuraTheme.Holy => new Color("fff9d8"),
            AuraTheme.Frost => new Color("dbfbff"),
            AuraTheme.Earth => new Color("f0cf97"),
            AuraTheme.Shadow => new Color("d4b6ff"),
            _ => Colors.White,
        };
    }

    public static IEnumerable<GlowLayer> EnumerateReplicaLayers(int elementalTags, int objType, float pulse, float ticks, float strength = 1f)
    {
        if (ItemAffixes.ElementMask(elementalTags) == 0)
        {
            yield break;
        }

        var primary = PrimaryColor(elementalTags);
        var accent = AccentColor(elementalTags);
        var drift = objType == 2 ? 1.85f : 1.05f;
        var grow = objType == 2 ? 1.7f : 0.9f;
        var swayX = Mathf.Sin(ticks / 105f) * drift;
        var swayY = Mathf.Cos(ticks / 135f) * drift * 0.55f;

        yield return new GlowLayer(WithAlpha(primary, (0.22f + pulse * 0.1f) * strength), grow * strength,
            new Vector2(-drift + swayX * 0.35f, -0.5f + swayY * 0.2f));
        yield return new GlowLayer(WithAlpha(accent, (0.18f + pulse * 0.08f) * strength), grow * 0.72f * strength,
            new Vector2(drift * 0.9f - swayX * 0.28f, 0.35f - swayY * 0.18f));
        yield return new GlowLayer(WithAlpha(primary.Lerp(Colors.White, 0.22f), (0.13f + pulse * 0.06f) * strength),
            grow * 0.4f * strength,
            new Vector2(swayX * 0.18f, -drift * 0.45f + swayY * 0.25f));
    }

    public static IEnumerable<GlowLayer> EnumerateHighlightLayers(int elementalTags, int objType, float pulse, float ticks, float strength = 1f)
    {
        if (ItemAffixes.ElementMask(elementalTags) == 0)
        {
            yield break;
        }

        var highlight = AccentColor(elementalTags).Lerp(Colors.White, 0.45f);
        var drift = objType == 2 ? 0.9f : 0.5f;
        yield return new GlowLayer(WithAlpha(highlight, (0.16f + pulse * 0.08f) * strength), 0f,
            new Vector2(Mathf.Sin(ticks / 80f) * drift, Mathf.Cos(ticks / 92f) * drift * 0.55f));
    }

    public static void DrawSparkles(CanvasItem canvas, Rect2 dest, int elementalTags, int objType, float pulse, float ticks, float strength = 1f)
    {
        if (ItemAffixes.ElementMask(elementalTags) == 0)
        {
            return;
        }

        switch (ThemeFor(elementalTags))
        {
            case AuraTheme.Blood:
                DrawBloodGlobules(canvas, dest, objType, pulse, ticks, strength, elementalTags);
                break;
            case AuraTheme.Lightning:
            case AuraTheme.Holy:
            case AuraTheme.Frost:
                DrawStarSparkles(canvas, dest, objType, pulse, ticks, strength, elementalTags, dense: true);
                break;
            case AuraTheme.Shadow:
                DrawStarSparkles(canvas, dest, objType, pulse, ticks, strength, elementalTags, dense: false);
                break;
            default:
                DrawOrbSparkles(canvas, dest, objType, pulse, ticks, strength, elementalTags);
                break;
        }
    }

    private static void DrawFireBackdrop(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        var main = EffectRect(dest, objType, 1.7f, 2.0f, 0.5f, 0.58f, new Vector2(26f, 40f), 1f);
        var outer = EffectRect(dest, objType, 1.15f, 1.35f, 0.56f, 0.34f, new Vector2(18f, 20f), 0.84f);
        var frame = (int)(ticks / 44f) % 30;
        DrawSpriteFrame(canvas, FlameParticleSheet(), 6, 5, frame, main,
            WithAlpha(new Color(1f, 0.42f, 0.12f), (0.88f + pulse * 0.12f) * strength));
        DrawSpriteFrame(canvas, FlameParticleSheet(), 6, 5, (frame + 9) % 30, outer,
            WithAlpha(new Color(1f, 0.78f, 0.22f), 0.52f * strength));
    }

    private static void DrawFireFront(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        var burst = EffectRect(dest, objType, 1.18f, 1.18f, 0.58f, 0.22f, new Vector2(16f, 16f), 0.7f);
        DrawSpriteFrame(canvas, FlameBurstSheet(), 8, 8, (int)(ticks / 20f) % 64, burst,
            WithAlpha(new Color(1f, 0.96f, 0.62f), (0.76f + pulse * 0.12f) * strength));
        DrawOrb(canvas, burst.GetCenter(), 1.6f * strength, WithAlpha(new Color(1f, 1f, 0.86f), 0.72f));
    }

    private static void DrawLightningBackdrop(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        var frame = (int)(ticks / 28f) % 8;
        var left = EffectRect(dest, objType, 0.5f, 2.2f, 0.22f, 0.54f, new Vector2(14f, 42f), 0.95f);
        var mid = EffectRect(dest, objType, 0.46f, 2.0f, 0.52f, 0.5f, new Vector2(12f, 38f), 1f);
        var right = EffectRect(dest, objType, 0.42f, 1.7f, 0.78f, 0.46f, new Vector2(12f, 32f), 0.85f);
        DrawSpriteFrame(canvas, LightningSheet(), 8, 1, frame, left,
            WithAlpha(new Color(0.36f, 0.78f, 1f), (0.82f + pulse * 0.08f) * strength));
        DrawSpriteFrame(canvas, LightningSheet(), 8, 1, (frame + 2) % 8, mid,
            WithAlpha(new Color(0.94f, 0.99f, 1f), 0.74f * strength));
        DrawSpriteFrame(canvas, LightningSheet(), 8, 1, (frame + 5) % 8, right,
            WithAlpha(new Color(0.44f, 0.88f, 1f), 0.52f * strength));
    }

    private static void DrawLightningFront(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        var a = dest.Position + new Vector2(dest.Size.X * 0.18f, dest.Size.Y * 0.9f);
        var b = dest.Position + new Vector2(dest.Size.X * 0.34f, dest.Size.Y * 0.58f + Mathf.Sin(ticks / 60f) * 2.6f * strength);
        var c = dest.Position + new Vector2(dest.Size.X * 0.54f, dest.Size.Y * 0.4f - Mathf.Cos(ticks / 55f) * 2.1f * strength);
        var d = dest.Position + new Vector2(dest.Size.X * 0.76f, dest.Size.Y * 0.12f);
        DrawBoltSegment(canvas, a, b, WithAlpha(new Color(0.48f, 0.84f, 1f), 0.88f * strength), 1.9f * strength);
        DrawBoltSegment(canvas, b, c, WithAlpha(new Color(1f, 1f, 1f), 0.94f * strength), 1.5f * strength);
        DrawBoltSegment(canvas, c, d, WithAlpha(new Color(0.48f, 0.84f, 1f), 0.82f * strength), 1.7f * strength);
        DrawOrb(canvas, d, 1.6f * strength, WithAlpha(new Color(1f, 1f, 1f), 0.92f));
    }

    private static void DrawBloodBackdrop(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        var frame = (int)(ticks / 70f) % 8;
        var smear = EffectRect(dest, objType, 1.65f, 1.8f, 0.56f, 0.54f, new Vector2(24f, 34f), 0.95f);
        var streak = EffectRect(dest, objType, 0.9f, 1.45f, 0.62f, 0.34f, new Vector2(12f, 22f), 0.8f);
        DrawSpriteFrame(canvas, BloodSheet(), 4, 2, frame, smear,
            WithAlpha(new Color(0.86f, 0.08f, 0.12f), (0.76f + pulse * 0.1f) * strength));
        DrawSpriteFrame(canvas, BloodSheet(), 4, 2, (frame + 3) % 8, streak,
            WithAlpha(new Color(0.28f, 0.02f, 0.03f), 0.52f * strength));
    }

    private static void DrawBloodFront(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        var tip = EffectRect(dest, objType, 0.92f, 0.92f, 0.68f, 0.18f, new Vector2(14f, 14f), 0.65f);
        DrawSpriteFrame(canvas, BloodSheet(), 4, 2, (int)(ticks / 80f + 5f) % 8, tip,
            WithAlpha(new Color(0.98f, 0.16f, 0.18f), (0.58f + pulse * 0.12f) * strength));
        var dripStart = tip.GetCenter() + new Vector2(-1f, 2f * strength);
        var dripEnd = dripStart + new Vector2(0f, 5f * strength + pulse * 2.4f);
        DrawBoltSegment(canvas, dripStart, dripEnd, WithAlpha(new Color(0.56f, 0.04f, 0.06f), 0.72f * strength), 1.2f * strength);
        DrawOrb(canvas, dripEnd, 1.3f * strength, WithAlpha(new Color(0.82f, 0.08f, 0.1f), 0.84f));
    }

    private static void DrawHolyBackdrop(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        var halo = EffectRect(dest, objType, 1.2f, 1.2f, 0.5f, 0.44f, new Vector2(18f, 18f), 0.7f);
        DrawSpriteFrame(canvas, FlameBurstSheet(), 8, 8, (int)(ticks / 22f) % 64, halo,
            WithAlpha(new Color(1f, 0.96f, 0.7f), (0.38f + pulse * 0.1f) * strength));
    }

    private static void DrawHolyFront(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        var center = dest.GetCenter();
        canvas.DrawLine(center + new Vector2(-dest.Size.X * 0.34f, 0f), center + new Vector2(dest.Size.X * 0.34f, 0f),
            WithAlpha(new Color(1f, 0.98f, 0.84f), 0.52f * strength), 1.3f * strength, true);
        canvas.DrawLine(center + new Vector2(0f, -dest.Size.Y * 0.42f), center + new Vector2(0f, dest.Size.Y * 0.42f),
            WithAlpha(new Color(1f, 1f, 0.94f), 0.58f * strength), 1.3f * strength, true);
    }

    private static void DrawFrostBackdrop(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        var left = EffectRect(dest, objType, 0.45f, 1.4f, 0.28f, 0.48f, new Vector2(10f, 22f), 0.75f);
        var right = EffectRect(dest, objType, 0.4f, 1.25f, 0.74f, 0.34f, new Vector2(9f, 20f), 0.7f);
        DrawSpriteFrame(canvas, LightningSheet(), 8, 1, (int)(ticks / 50f) % 8, left,
            WithAlpha(new Color(0.72f, 0.94f, 1f), 0.28f * strength));
        DrawSpriteFrame(canvas, LightningSheet(), 8, 1, (int)(ticks / 50f + 4f) % 8, right,
            WithAlpha(new Color(0.88f, 1f, 1f), 0.18f * strength));
    }

    private static void DrawFrostFront(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        var left = dest.Position + new Vector2(dest.Size.X * 0.18f, dest.Size.Y * 0.3f);
        var right = dest.Position + new Vector2(dest.Size.X * 0.82f, dest.Size.Y * 0.18f);
        var lower = dest.Position + new Vector2(dest.Size.X * 0.44f, dest.Size.Y * 0.74f);
        DrawBoltSegment(canvas, left, right, WithAlpha(new Color(0.78f, 0.98f, 1f), 0.4f * strength), 1.2f * strength);
        DrawBoltSegment(canvas, lower, right, WithAlpha(new Color(0.54f, 0.82f, 1f), 0.28f * strength), 1.0f * strength);
    }

    private static void DrawEarthBackdrop(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        var left = dest.Position + new Vector2(dest.Size.X * 0.26f, dest.Size.Y * 0.82f);
        var right = dest.Position + new Vector2(dest.Size.X * 0.74f, dest.Size.Y * 0.8f);
        DrawOrb(canvas, left + new Vector2(Mathf.Sin(ticks / 100f) * 1.6f, 0f), 1.7f * strength, WithAlpha(new Color(0.78f, 0.58f, 0.24f), 0.5f));
        DrawOrb(canvas, right + new Vector2(Mathf.Cos(ticks / 120f) * 1.5f, -1f), 1.4f * strength, WithAlpha(new Color(0.95f, 0.76f, 0.44f), 0.42f));
    }

    private static void DrawEarthFront(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        var left = dest.Position + new Vector2(dest.Size.X * 0.26f, dest.Size.Y * 0.82f);
        var right = dest.Position + new Vector2(dest.Size.X * 0.74f, dest.Size.Y * 0.8f);
        canvas.DrawLine(left, right, WithAlpha(new Color(0.9f, 0.7f, 0.38f), 0.26f * strength), 1.1f * strength, true);
    }

    private static void DrawShadowBackdrop(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        var wispA = EffectRect(dest, objType, 1.2f, 1.4f, 0.34f, 0.56f, new Vector2(16f, 22f), 0.8f);
        var wispB = EffectRect(dest, objType, 1.0f, 1.25f, 0.68f, 0.38f, new Vector2(14f, 20f), 0.72f);
        DrawSpriteFrame(canvas, BloodSheet(), 4, 2, (int)(ticks / 86f) % 8, wispA,
            WithAlpha(new Color(0.36f, 0.1f, 0.62f), 0.3f * strength));
        DrawSpriteFrame(canvas, BloodSheet(), 4, 2, (int)(ticks / 70f + 5f) % 8, wispB,
            WithAlpha(new Color(0.08f, 0.02f, 0.14f), 0.42f * strength));
    }

    private static void DrawShadowFront(CanvasItem canvas, Rect2 dest, float pulse, float ticks, int objType, float strength)
    {
        DrawOrb(canvas, dest.GetCenter() + new Vector2(0f, dest.Size.Y * 0.3f), 1.5f * strength,
            WithAlpha(new Color(0.76f, 0.1f, 0.18f), 0.44f + pulse * 0.08f));
    }

    private static Rect2 EffectRect(Rect2 dest, int objType, float widthScale, float heightScale, float anchorX, float anchorY, Vector2 minSize, float strength)
    {
        var size = new Vector2(
            MathF.Max(dest.Size.X * widthScale, minSize.X * strength),
            MathF.Max(dest.Size.Y * heightScale, minSize.Y * strength));
        if (objType != 2)
        {
            size *= new Vector2(0.86f, 0.82f);
        }
        var pos = dest.Position + new Vector2(dest.Size.X * anchorX - size.X * 0.5f, dest.Size.Y * anchorY - size.Y * 0.5f);
        return new Rect2(pos, size);
    }

    private static void DrawBoltSegment(CanvasItem canvas, Vector2 from, Vector2 to, Color color, float width) =>
        canvas.DrawLine(from, to, color, Mathf.Max(0.8f, width), true);

    private static void DrawSpriteFrame(CanvasItem canvas, Texture2D? texture, int columns, int rows, int frame, Rect2 dest, Color modulate)
    {
        if (texture is null || columns <= 0 || rows <= 0)
        {
            return;
        }

        var frameWidth = texture.GetWidth() / columns;
        var frameHeight = texture.GetHeight() / rows;
        if (frameWidth <= 0 || frameHeight <= 0)
        {
            return;
        }

        var count = columns * rows;
        frame = Mathf.PosMod(frame, count);
        var col = frame % columns;
        var row = frame / columns;
        var src = new Rect2(col * frameWidth, row * frameHeight, frameWidth, frameHeight);
        canvas.DrawTextureRectRegion(texture, dest, src, modulate);
    }

    private static Texture2D? LightningSheet() => _lightningSheet ??= LoadTexture("res://assets/fx/item_auras/lightning_sheet.png");
    private static Texture2D? BloodSheet() => _bloodSheet ??= LoadTexture("res://assets/fx/item_auras/blood_splatter_sheet.png");
    private static Texture2D? FlameParticleSheet() => _flameParticleSheet ??= LoadTexture("res://assets/fx/item_auras/flame_particle.png");
    private static Texture2D? FlameBurstSheet() => _flameBurstSheet ??= LoadTexture("res://assets/fx/item_auras/flame_burst.png");

    private static Texture2D? LoadTexture(string path)
    {
        if (!ResourceLoader.Exists(path))
        {
            return null;
        }
        return ResourceLoader.Load<Texture2D>(path);
    }

    private static void DrawOrb(CanvasItem canvas, Vector2 center, float radius, Color color) =>
        canvas.DrawCircle(center, Mathf.Max(0.8f, radius), color);

    private static Color WithAlpha(Color color, float alpha) =>
        new(color.R, color.G, color.B, Mathf.Clamp(alpha, 0f, 1f));

    private static void DrawStarSparkles(CanvasItem canvas, Rect2 dest, int objType, float pulse, float ticks, float strength, int elementalTags, bool dense)
    {
        var primary = PrimaryColor(elementalTags);
        var accent = AccentColor(elementalTags);
        var count = dense ? (objType == 2 ? 8 : 6) : (objType == 2 ? 6 : 4);
        for (var i = 0; i < count; i++)
        {
            var phase = ticks / 230f + i * 1.37f;
            var xNorm = 0.16f + 0.68f * Hash01(i * 37 + ThemeSeed(elementalTags) * 19);
            var yNorm = 0.08f + 0.88f * Hash01(i * 61 + ThemeSeed(elementalTags) * 13);
            var driftX = Mathf.Sin(phase * (1.2f + i * 0.08f)) * (objType == 2 ? 1.8f : 0.9f) * strength;
            var driftY = Mathf.Cos(phase * (1.05f + i * 0.06f)) * (objType == 2 ? 1.2f : 0.65f) * strength;
            var center = dest.Position + new Vector2(dest.Size.X * xNorm + driftX, dest.Size.Y * yNorm + driftY);
            var sparklePulse = 0.45f + 0.55f * Mathf.Sin(phase * 1.9f + i);
            var radius = (objType == 2 ? 1.05f : 0.72f) * strength + sparklePulse * 0.55f;
            var color = i % 2 == 0 ? primary : accent;
            DrawOrb(canvas, center, radius, WithAlpha(color, (0.2f + sparklePulse * 0.2f) * strength));

            if (sparklePulse > (dense ? 0.58f : 0.66f))
            {
                var starColor = WithAlpha(accent.Lerp(Colors.White, 0.35f), (0.2f + sparklePulse * 0.2f) * strength);
                var arm = radius * (objType == 2 ? 2.1f : 1.6f);
                canvas.DrawLine(center + new Vector2(-arm, 0f), center + new Vector2(arm, 0f), starColor, 0.8f, true);
                canvas.DrawLine(center + new Vector2(0f, -arm), center + new Vector2(0f, arm), starColor, 0.8f, true);
            }
        }
    }

    private static void DrawOrbSparkles(CanvasItem canvas, Rect2 dest, int objType, float pulse, float ticks, float strength, int elementalTags)
    {
        var primary = PrimaryColor(elementalTags);
        var accent = AccentColor(elementalTags);
        var count = objType == 2 ? 7 : 5;
        for (var i = 0; i < count; i++)
        {
            var phase = ticks / 245f + i * 1.1f;
            var xNorm = 0.18f + 0.64f * Hash01(i * 29 + ThemeSeed(elementalTags) * 17);
            var yNorm = 0.1f + 0.84f * Hash01(i * 47 + ThemeSeed(elementalTags) * 23);
            var drift = new Vector2(
                Mathf.Sin(phase * (1.1f + i * 0.07f)) * (objType == 2 ? 1.5f : 0.8f) * strength,
                Mathf.Cos(phase * (0.95f + i * 0.05f)) * (objType == 2 ? 1.0f : 0.55f) * strength);
            var center = dest.Position + new Vector2(dest.Size.X * xNorm, dest.Size.Y * yNorm) + drift;
            var alphaPulse = 0.42f + 0.58f * Mathf.Sin(phase * 1.6f + i * 0.8f);
            var radius = (objType == 2 ? 1f : 0.68f) * strength + alphaPulse * 0.45f;
            DrawOrb(canvas, center, radius, WithAlpha(i % 2 == 0 ? primary : accent, (0.22f + alphaPulse * 0.2f) * strength));
        }
    }

    private static void DrawBloodGlobules(CanvasItem canvas, Rect2 dest, int objType, float pulse, float ticks, float strength, int elementalTags)
    {
        var primary = PrimaryColor(elementalTags);
        var accent = AccentColor(elementalTags);
        var dark = new Color(0.24f, 0.02f, 0.03f);
        var count = objType == 2 ? 8 : 6;
        for (var i = 0; i < count; i++)
        {
            var phase = ticks / 210f + i * 0.92f;
            var xNorm = 0.18f + 0.62f * Hash01(i * 31 + ThemeSeed(elementalTags) * 11);
            var yNorm = 0.16f + 0.72f * Hash01(i * 43 + ThemeSeed(elementalTags) * 29);
            var swingX = Mathf.Sin(phase * (0.95f + i * 0.05f)) * (objType == 2 ? 1.3f : 0.7f) * strength;
            var dripY = Mathf.Abs(Mathf.Cos(phase * (0.7f + i * 0.04f))) * (objType == 2 ? 1.4f : 0.8f) * strength;
            var center = dest.Position + new Vector2(dest.Size.X * xNorm + swingX, dest.Size.Y * yNorm + dripY);
            var blobPulse = 0.35f + 0.65f * Mathf.Sin(phase * 1.4f + i * 0.5f);
            var radius = (objType == 2 ? 1.2f : 0.85f) * strength + blobPulse * 0.5f;
            var blobColor = i % 3 == 0 ? accent : (i % 2 == 0 ? primary : dark);
            DrawOrb(canvas, center, radius, WithAlpha(blobColor, (0.24f + blobPulse * 0.24f) * strength));

            if (blobPulse > 0.62f)
            {
                var tail = center + new Vector2(0f, radius * (objType == 2 ? 1.8f : 1.4f));
                canvas.DrawLine(center, tail, WithAlpha(dark, 0.26f * strength), 0.9f, true);
                DrawOrb(canvas, tail, radius * 0.55f, WithAlpha(primary, 0.2f * strength));
            }
        }
    }

    private static float Hash01(int seed)
    {
        var value = Mathf.Sin(seed * 12.9898f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }

    private static int ThemeSeed(int elementalTags) => ThemeFor(elementalTags) switch
    {
        AuraTheme.Fire => 11,
        AuraTheme.Lightning => 23,
        AuraTheme.Blood => 31,
        AuraTheme.Holy => 43,
        AuraTheme.Frost => 59,
        AuraTheme.Earth => 67,
        AuraTheme.Shadow => 79,
        _ => 3,
    };

    private static float ScaledStrength(int objType, float strength) =>
        objType == 2 ? strength : strength * 0.84f;

    private static AuraTheme ThemeFor(int elementalTags)
    {
        var elemental = ItemAffixes.ElementMask(elementalTags);
        if ((elemental & ItemAffixes.ElementFire) != 0) return AuraTheme.Fire;
        if ((elemental & ItemAffixes.ElementWind) != 0) return AuraTheme.Lightning;
        if ((elemental & ItemAffixes.ElementChaos) != 0) return AuraTheme.Blood;
        if ((elemental & ItemAffixes.ElementDark) != 0) return AuraTheme.Shadow;
        if ((elemental & ItemAffixes.ElementLight) != 0) return AuraTheme.Holy;
        if ((elemental & ItemAffixes.ElementWater) != 0) return AuraTheme.Frost;
        if ((elemental & ItemAffixes.ElementEarth) != 0) return AuraTheme.Earth;
        return AuraTheme.None;
    }

    private enum AuraTheme
    {
        None,
        Fire,
        Lightning,
        Blood,
        Holy,
        Frost,
        Earth,
        Shadow,
    }
}
