using System;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>Hosts a VB6 form background at native pixel size with child controls in form-local coords.</summary>
public partial class Vb6FormShell : Control
{
    private const string MetaDesignPos = "vb6_design_pos";
    private const string MetaDesignSize = "vb6_design_size";

    private readonly TextureRect _background = new();
    private Vector2 _stretchScale = Vector2.One;
    private StyleBoxFlat? _panelStyle;

    public Vector2 DesignSize { get; private set; }

    public Vb6FormShell()
    {
        ClipContents = true;
        TextureFilter = TextureFilterEnum.Linear;
        _background.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _background.StretchMode = TextureRect.StretchModeEnum.Scale;
        _background.TextureFilter = TextureFilterEnum.Nearest;
        AddChild(_background);
    }

    public void ApplyPixelScale(int pixelScale)
    {
        ApplyStretch(new Vector2(pixelScale, pixelScale));
    }

    public void ApplyStretch(AoUiScale scale)
    {
        ApplyStretch(scale.Scale);
    }

    public void ApplyStretch(Vector2 stretchScale)
    {
        _stretchScale = stretchScale;
        Scale = Vector2.One;
        Size = DesignSize * _stretchScale;
        CustomMinimumSize = Size;
        _background.Position = Vector2.Zero;
        _background.Scale = Vector2.One;
        _background.Size = Size;

        foreach (var child in GetChildren())
        {
            if (child == _background || child is not Control control)
            {
                continue;
            }
            if (!control.HasMeta(MetaDesignPos))
            {
                continue;
            }
            var designPos = (Vector2)control.GetMeta(MetaDesignPos);
            var designSize = (Vector2)control.GetMeta(MetaDesignSize);
            control.Position = new Vector2(designPos.X * _stretchScale.X, designPos.Y * _stretchScale.Y);
            control.Scale = Vector2.One;
            if (control is GraphicalButton button && button.TextureNormal is not null)
            {
                var texSize = button.TextureNormal.GetSize();
                control.Size = new Vector2(texSize.X * _stretchScale.X, texSize.Y * _stretchScale.Y);
            }
            else
            {
                control.Size = new Vector2(designSize.X * _stretchScale.X, designSize.Y * _stretchScale.Y);
            }
            ApplyTextScale(control);
        }

        QueueRedraw();
    }

    private void TagDesign(Control control, Vector2 position, Vector2 size)
    {
        control.SetMeta(MetaDesignPos, position);
        control.SetMeta(MetaDesignSize, size);
    }

    private void ApplyTextScale(Control control)
    {
        if (control is not LineEdit and not Label and not Button and not CheckBox)
        {
            return;
        }
        control.TextureFilter = TextureFilterEnum.Linear;
        control.AddThemeFontOverride("font", AoUiFonts.Ui);
        control.AddThemeFontSizeOverride("font_size", 11);
    }

    public override void _Draw()
    {
        if (_background.Texture is null && _panelStyle is not null)
        {
            DrawStyleBox(_panelStyle, new Rect2(Vector2.Zero, Size));
        }
    }

    public void SetBackground(Texture2D? texture)
    {
        _background.Texture = texture;
        if (texture is null)
        {
            QueueRedraw();
            return;
        }
        DesignSize = texture.GetSize();
        CustomMinimumSize = DesignSize;
        Size = DesignSize;
        _background.Size = DesignSize;
        QueueRedraw();
    }

    public void SetDesignSize(float width, float height)
    {
        DesignSize = new Vector2(width, height);
        CustomMinimumSize = DesignSize;
        Size = DesignSize;
        _background.Size = DesignSize;
        QueueRedraw();
    }

    public void SetPanelStyle(Color? fillColor = null, Color? borderColor = null)
    {
        _panelStyle = new StyleBoxFlat
        {
            BgColor = fillColor ?? new Color(0.05f, 0.08f, 0.12f, 0.84f),
            BorderColor = borderColor ?? new Color(0.82f, 0.72f, 0.5f, 0.92f),
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            ShadowColor = new Color(0, 0, 0, 0.35f),
            ShadowSize = 8,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 12,
            ContentMarginBottom = 12,
        };
        QueueRedraw();
    }

    public LineEdit AddField(int left, int top, int width, int height, bool secret = false, int maxLength = 0)
    {
        var field = new LineEdit
        {
            Position = Vb6FormCoords.Position(left, top),
            Size = Vb6FormCoords.Size(width, height),
            Secret = secret,
            MaxLength = maxLength,
            CaretBlink = true,
        };
        StyleField(field);
        field.TextureFilter = TextureFilterEnum.Linear;
        TagDesign(field, field.Position, field.Size);
        AddChild(field);
        return field;
    }

    public GraphicalButton AddButton(
        InterfaceLoader ui,
        string baseName,
        int left,
        int top,
        int width,
        int height,
        Action onPress)
    {
        var button = new GraphicalButton
        {
            Position = Vb6FormCoords.Position(left, top),
            Size = Vb6FormCoords.Size(width, height),
        };
        button.Initialize(
            ui.LoadSpanish($"{baseName}-default.bmp"),
            ui.LoadSpanish($"{baseName}-over.bmp"),
            ui.LoadSpanish($"{baseName}-off.bmp"));
        button.Pressed += onPress;
        if (button.TextureNormal is not null)
        {
            button.Size = button.TextureNormal.GetSize();
        }
        TagDesign(button, button.Position, button.Size);
        AddChild(button);
        return button;
    }

    public TextureButton AddCheckbox(InterfaceLoader ui, int left, int top, int width, int height, Action<bool> onToggle)
    {
        var on = ui.LoadSpanish("check-amarillo.bmp") ?? ui.Load("check-amarillo.bmp");
        var button = new TextureButton
        {
            Position = Vb6FormCoords.Position(left, top),
            Size = Vb6FormCoords.Size(width, height),
            ToggleMode = true,
            TextureNormal = null,
            TexturePressed = on,
            IgnoreTextureSize = true,
            StretchMode = TextureButton.StretchModeEnum.Keep,
        };
        button.Toggled += pressed => onToggle(pressed);
        TagDesign(button, button.Position, button.Size);
        AddChild(button);
        return button;
    }

    public Label AddCaption(int left, int top, int width, int height, string text, Color? color = null)
    {
        var label = new Label
        {
            Position = Vb6FormCoords.Position(left, top),
            Size = Vb6FormCoords.Size(width, height),
            Text = text,
            Modulate = color ?? new Color("c0c0c0"),
        };
        label.AddThemeFontOverride("font", AoUiFonts.Ui);
        label.AddThemeFontSizeOverride("font_size", 11);
        label.TextureFilter = TextureFilterEnum.Linear;
        TagDesign(label, label.Position, label.Size);
        AddChild(label);
        return label;
    }

    public Button AddTextButton(int left, int top, int width, int height, string text, Action onPress, bool primary = false)
    {
        var button = new Button
        {
            Position = Vb6FormCoords.Position(left, top),
            Size = Vb6FormCoords.Size(width, height),
            Text = text,
            Flat = false,
            FocusMode = FocusModeEnum.None,
        };
        StyleButton(button, primary);
        button.Pressed += onPress;
        TagDesign(button, button.Position, button.Size);
        AddChild(button);
        return button;
    }

    public CheckBox AddTextCheckbox(int left, int top, int width, int height, string text, Action<bool> onToggle)
    {
        var checkbox = new CheckBox
        {
            Position = Vb6FormCoords.Position(left, top),
            Size = Vb6FormCoords.Size(width, height),
            Text = text,
            FocusMode = FocusModeEnum.None,
        };
        StyleCheckbox(checkbox);
        checkbox.Toggled += pressed => onToggle(pressed);
        TagDesign(checkbox, checkbox.Position, checkbox.Size);
        AddChild(checkbox);
        return checkbox;
    }

    public static void StyleField(LineEdit field)
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.06f, 0.09f, 0.9f),
            BorderColor = new Color(0.76f, 0.67f, 0.48f, 0.95f),
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
        var focused = style.Duplicate() as StyleBoxFlat ?? new StyleBoxFlat();
        focused.BorderColor = new Color(0.95f, 0.82f, 0.56f, 1f);
        focused.ShadowColor = new Color(0.95f, 0.82f, 0.56f, 0.18f);
        focused.ShadowSize = 4;
        field.AddThemeStyleboxOverride("normal", style);
        field.AddThemeStyleboxOverride("focus", focused);
        field.AddThemeStyleboxOverride("read_only", style);
        field.AddThemeColorOverride("font_color", new Color(0.95f, 0.95f, 0.95f));
        field.AddThemeColorOverride("font_placeholder_color", new Color(0.62f, 0.66f, 0.72f));
        field.AddThemeColorOverride("caret_color", new Color(0.95f, 0.82f, 0.56f));
        field.AddThemeFontOverride("font", AoUiFonts.Ui);
        field.AddThemeFontSizeOverride("font_size", 11);
    }

    public static void StyleButton(Button button, bool primary = false)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = primary ? new Color(0.54f, 0.18f, 0.14f, 0.92f) : new Color(0.08f, 0.11f, 0.16f, 0.88f),
            BorderColor = primary ? new Color(0.95f, 0.74f, 0.56f, 1f) : new Color(0.72f, 0.66f, 0.5f, 0.85f),
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
        var hover = normal.Duplicate() as StyleBoxFlat ?? new StyleBoxFlat();
        hover.BgColor = primary ? new Color(0.64f, 0.22f, 0.17f, 0.96f) : new Color(0.12f, 0.16f, 0.22f, 0.94f);
        hover.BorderColor = new Color(0.96f, 0.82f, 0.6f, 1f);
        var pressed = hover.Duplicate() as StyleBoxFlat ?? new StyleBoxFlat();
        pressed.BgColor = primary ? new Color(0.42f, 0.15f, 0.11f, 0.96f) : new Color(0.06f, 0.09f, 0.14f, 0.96f);
        var disabled = normal.Duplicate() as StyleBoxFlat ?? new StyleBoxFlat();
        disabled.BgColor = new Color(0.08f, 0.08f, 0.08f, 0.55f);
        disabled.BorderColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("disabled", disabled);
        button.AddThemeColorOverride("font_color", new Color(0.96f, 0.96f, 0.96f));
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeColorOverride("font_disabled_color", new Color(0.7f, 0.7f, 0.7f, 0.75f));
        button.AddThemeFontOverride("font", AoUiFonts.Ui);
        button.AddThemeFontSizeOverride("font_size", 11);
    }

    public static void StyleCheckbox(CheckBox checkbox)
    {
        checkbox.AddThemeFontOverride("font", AoUiFonts.Ui);
        checkbox.AddThemeFontSizeOverride("font_size", 11);
        checkbox.AddThemeColorOverride("font_color", new Color(0.88f, 0.9f, 0.93f));
        checkbox.AddThemeColorOverride("font_hover_color", Colors.White);
        checkbox.AddThemeColorOverride("font_pressed_color", Colors.White);
    }
}
