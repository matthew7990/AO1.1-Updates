using System;
using System.Collections.Generic;
using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>Panel de creación de PJ — el mapa de la ciudad lo dibuja WorldView (como selección de personajes).</summary>
public partial class CreateCharacterScreen : Control
{
    private static readonly Color PanelFill = new(0.06f, 0.07f, 0.11f, 0.88f);
    private static readonly Color PanelBorder = new(0.72f, 0.62f, 0.38f, 0.55f);
    private static readonly Color TextPrimary = new("ebe4d6");
    private static readonly Color TextMuted = new(0.58f, 0.56f, 0.52f);
    private static readonly Color BtnPrimary = new("c9a441");
    private static readonly Color BtnGhost = new(0.14f, 0.15f, 0.2f, 0.9f);

    private CharacterCreationCatalog? _catalog;

    private LineEdit _nameField = null!;
    private Button _backBtn = null!;
    private Button _createBtn = null!;
    private Control _panelHit = null!;
    private Label _messageLabel = null!;
    private bool _createInFlight;
    private int _raceIndex = 1;
    private int _genderIndex = 1;
    private int _classIndex = 1;
    private int _homeIndex = 1;
    private int _headIndex;
    private int _heading = 3;
    private IReadOnlyList<int> _headOptions = [1];

    private Rect2 _panelRect;
    private readonly Dictionary<string, Rect2> _selectorRects = new();

    public int PreviewHomeId => _homeIndex;
    public int PreviewHeading => _heading;
    public int PreviewBodyId => _catalog?.GetBodyId(_raceIndex, _genderIndex == 1) ?? 1;
    public int PreviewHeadId => _headOptions.Count > 0 ? _headOptions[_headIndex] : 0;

    public event Action? BackRequested;
    public event Action? CreateRequested;
    public event Action? SelectionChanged;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        TextureFilter = TextureFilterEnum.Nearest;
        Visible = false;

        _nameField = new LineEdit
        {
            MaxLength = 18,
            PlaceholderText = "Nombre del personaje",
            CaretBlink = true,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _nameField.AddThemeFontOverride("font", AoUiFonts.Ui);
        _nameField.AddThemeColorOverride("font_color", TextPrimary);
        _nameField.AddThemeColorOverride("font_placeholder_color", TextMuted);
        _nameField.TextChanged += _ =>
        {
            SetMessage(null);
            NotifySelectionChanged();
        };
        _nameField.TextSubmitted += _ => CreateRequested?.Invoke();
        AddChild(_nameField);

        _backBtn = MakeActionButton("Volver", BtnGhost, TextPrimary);
        _backBtn.Pressed += () => BackRequested?.Invoke();
        _backBtn.ZIndex = 2;
        AddChild(_backBtn);

        _createBtn = MakeActionButton("Crear", BtnPrimary, new Color(0.12f, 0.1f, 0.06f));
        _createBtn.Pressed += OnCreatePressed;
        _createBtn.ZIndex = 2;
        AddChild(_createBtn);

        _messageLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        _messageLabel.AddThemeFontOverride("font", AoUiFonts.Ui);
        _messageLabel.AddThemeFontSizeOverride("font_size", 11);
        _messageLabel.AddThemeColorOverride("font_color", new Color("e05a5a"));
        AddChild(_messageLabel);

        _panelHit = new Control { MouseFilter = MouseFilterEnum.Stop, ZIndex = 1 };
        _panelHit.GuiInput += OnPanelHitInput;
        AddChild(_panelHit);
        MoveChild(_panelHit, 0);
    }

    public void SetMessage(string? text)
    {
        var hasText = !string.IsNullOrWhiteSpace(text);
        _messageLabel.Visible = hasText;
        _messageLabel.Text = hasText ? text : string.Empty;
        if (hasText)
        {
            QueueRedraw();
        }
    }

    public void SetCreateEnabled(bool enabled)
    {
        _createInFlight = !enabled;
        _createBtn.Disabled = !enabled;
    }

    private void OnCreatePressed()
    {
        if (_createInFlight)
        {
            return;
        }
        CreateRequested?.Invoke();
    }

    private void OnPanelHitInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            return;
        }
        var pos = _panelHit.GetLocalMousePosition() + _panelHit.Position;
        foreach (var key in _selectorRects.Keys)
        {
            if (!_selectorRects.TryGetValue(key, out var rect) || !rect.HasPoint(pos))
            {
                continue;
            }
            HandleSelector(key, pos.X < rect.GetCenter().X ? -1 : 1);
            _panelHit.AcceptEvent();
            return;
        }
    }

    public void Configure(CharacterCreationCatalog catalog, GameResources? resources)
    {
        _catalog = catalog;
        _ = resources;
        CallDeferred(MethodName.EnsureLayout);
    }

    public void Reset()
    {
        _raceIndex = 1;
        _genderIndex = 1;
        _classIndex = 1;
        _homeIndex = 1;
        _heading = 3;
        _nameField.Text = "";
        _createInFlight = false;
        _createBtn.Disabled = false;
        SetMessage(null);
        RefreshHeadOptions();
        NotifySelectionChanged();
        CallDeferred(MethodName.EnsureLayout);
    }

    public string CharacterName => _nameField.Text.Trim();

    public bool TryGetSelection(out int race, out int gender, out int classId, out int head, out int home)
    {
        race = _raceIndex;
        gender = _genderIndex;
        classId = _classIndex;
        home = _homeIndex;
        head = PreviewHeadId;
        return _catalog is not null
               && _catalog.IsCreationComplete(race, gender, classId, head, home);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized || (what == NotificationVisibilityChanged && Visible))
        {
            EnsureLayout();
        }
    }

    private void EnsureLayout()
    {
        if (_catalog is null || !IsInsideTree() || _nameField is null)
        {
            return;
        }
        var viewport = GetViewportRect().Size;
        var panelW = viewport.X * 0.34f;
        var panelX = viewport.X - panelW - viewport.X * 0.02f;
        var panelY = viewport.Y * 0.05f;
        var panelH = viewport.Y * 0.9f;
        _panelRect = new Rect2(panelX, panelY, panelW, panelH);

        var inner = _panelRect.Grow(-24);
        _nameField.Position = new Vector2(inner.Position.X, inner.Position.Y + 52f);
        _nameField.Size = new Vector2(inner.Size.X, 34f);
        _nameField.AddThemeFontSizeOverride("font_size", 14);

        const float btnH = 42f;
        const float btnW = 140f;
        const float gap = 16f;
        var totalW = btnW * 2 + gap;
        var startX = _panelRect.Position.X + (_panelRect.Size.X - totalW) * 0.5f;
        var btnY = _panelRect.End.Y - btnH - 20f;
        const float footerH = 72f;
        _panelHit.Position = _panelRect.Position;
        _panelHit.Size = new Vector2(_panelRect.Size.X, MathF.Max(0f, _panelRect.Size.Y - footerH));

        _backBtn.Position = new Vector2(startX, btnY);
        _backBtn.Size = new Vector2(btnW, btnH);
        _backBtn.ZIndex = 2;
        _createBtn.Position = new Vector2(startX + btnW + gap, btnY);
        _createBtn.Size = new Vector2(btnW, btnH);

        var msgY = btnY - 34f;
        _messageLabel.Position = new Vector2(_panelRect.Position.X + 12f, msgY);
        _messageLabel.Size = new Vector2(_panelRect.Size.X - 24f, 30f);

        MoveChild(_panelHit, 0);
        MoveChild(_messageLabel, GetChildCount() - 1);
        MoveChild(_createBtn, GetChildCount() - 1);
        MoveChild(_backBtn, GetChildCount() - 1);

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_catalog is null || _panelRect.Size == Vector2.Zero)
        {
            return;
        }

        DrawRect(new Rect2(_panelRect.Position + new Vector2(2, 3), _panelRect.Size), new Color(0, 0, 0, 0.4f));
        DrawRect(_panelRect, PanelFill);
        DrawBorder(_panelRect, PanelBorder);
        DrawForm();
    }

    private static Button MakeActionButton(string text, Color fill, Color fontColor)
    {
        var btn = new Button
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.None,
        };
        btn.AddThemeFontOverride("font", AoUiFonts.Ui);
        btn.AddThemeFontSizeOverride("font_size", 13);
        btn.AddThemeColorOverride("font_color", fontColor);
        btn.AddThemeColorOverride("font_hover_color", fontColor);
        btn.AddThemeColorOverride("font_pressed_color", fontColor);
        var normal = new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = PanelBorder,
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
        };
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", normal);
        btn.AddThemeStyleboxOverride("pressed", normal);
        return btn;
    }

    private void HandleSelector(string key, int delta)
    {
        if (_catalog is null)
        {
            return;
        }
        switch (key)
        {
            case "race":
                Cycle(ref _raceIndex, _catalog.Races.Count - 1, delta);
                RefreshHeadOptions();
                break;
            case "gender":
                Cycle(ref _genderIndex, 2, delta);
                RefreshHeadOptions();
                break;
            case "class":
                Cycle(ref _classIndex, _catalog.Classes.Count - 1, delta);
                break;
            case "home":
                Cycle(ref _homeIndex, _catalog.Cities.Count - 1, delta);
                break;
            case "head":
                if (_headOptions.Count > 0)
                {
                    _headIndex = (_headIndex + delta + _headOptions.Count) % _headOptions.Count;
                }
                break;
            case "heading":
                CycleHeading(delta);
                break;
        }
        NotifySelectionChanged();
    }

    private void NotifySelectionChanged()
    {
        QueueRedraw();
        SelectionChanged?.Invoke();
    }

    private void CycleHeading(int delta)
    {
        _heading += delta;
        if (_heading < 1)
        {
            _heading = 4;
        }
        else if (_heading > 4)
        {
            _heading = 1;
        }
    }

    private void RefreshHeadOptions()
    {
        if (_catalog is null)
        {
            _headOptions = [1];
            _headIndex = 0;
            return;
        }
        _headOptions = _catalog.GetHeadOptions(_raceIndex, _genderIndex);
        _headIndex = 0;
    }

    private static void Cycle(ref int index, int maxIndex, int delta)
    {
        index += delta;
        if (index < 1)
        {
            index = maxIndex;
        }
        else if (index > maxIndex)
        {
            index = 1;
        }
    }

    private static string HeadingLabel(int heading) => heading switch
    {
        1 => "Norte",
        2 => "Este",
        3 => "Sur",
        4 => "Oeste",
        _ => "Sur",
    };

    private static string HeadLabel(int index, int total) =>
        total > 0 ? $"{index + 1} / {total}" : "—";

    private void DrawForm()
    {
        if (_catalog is null)
        {
            return;
        }

        var font = AoUiFonts.Ui;
        var x = _panelRect.Position.X + 24f;
        var y = _panelRect.Position.Y + 20f;
        var w = _panelRect.Size.X - 48f;

        HudTextDraw.AtTopLeft(this, AoUiFonts.Title, new Vector2(x, y), "Nuevo personaje", 20, TextPrimary);
        HudTextDraw.AtTopLeft(this, font, new Vector2(x, y + 28f),
            "El personaje aparece en tu ciudad natal", 11, TextMuted);

        y += 88f;
        _selectorRects.Clear();
        y = DrawSelector("race", "Raza", _catalog.Races[_raceIndex], x, y, w);
        y = DrawSelector("gender", "Género", _catalog.Genders[_genderIndex - 1], x, y, w);
        y = DrawSelector("class", "Clase", _catalog.Classes[_classIndex], x, y, w);
        y = DrawSelector("home", "Ciudad natal", _catalog.Cities[_homeIndex], x, y, w);
        y = DrawSelector("head", "Cabeza",
            HeadLabel(_headIndex, _headOptions.Count), x, y, w);
        y = DrawSelector("heading", "Orientación", HeadingLabel(_heading), x, y, w);

        y += 8f;
        DrawAttributes(x, y, w);
    }

    private float DrawSelector(string key, string label, string value, float x, float y, float w)
    {
        var font = AoUiFonts.Ui;
        const float rowH = 42f;
        var rect = new Rect2(x, y, w, rowH);
        _selectorRects[key] = rect;

        DrawRect(rect, new Color(1f, 1f, 1f, 0.04f));
        DrawBorder(rect, new Color(PanelBorder, 0.35f));

        HudTextDraw.AtTopLeft(this, font, new Vector2(x + 10f, y + 5f), label, 10, TextMuted);
        HudTextDraw.AtTopLeft(this, font, new Vector2(x + 10f, y + 21f), value, 13, TextPrimary);

        var arrowY = y + rowH * 0.5f + 4f;
        DrawString(font, new Vector2(x + 8f, arrowY), "‹", fontSize: 18, modulate: TextMuted);
        DrawString(font, new Vector2(x + w - 16f, arrowY), "›", fontSize: 18, modulate: TextMuted);

        return y + rowH + 6f;
    }

    private void DrawAttributes(float x, float y, float w)
    {
        if (_catalog is null)
        {
            return;
        }
        var mods = _catalog.GetRaceModifiers(_raceIndex);
        var font = AoUiFonts.Ui;
        HudTextDraw.AtTopLeft(this, font, new Vector2(x, y), "Atributos", 12, TextPrimary);
        y += 22f;
        var colW = w * 0.5f;
        DrawAttr(font, x, y, "FUE", mods.Fuerza);
        DrawAttr(font, x + colW, y, "AGI", mods.Agilidad);
        y += 22f;
        DrawAttr(font, x, y, "INT", mods.Inteligencia);
        DrawAttr(font, x + colW, y, "CON", mods.Constitucion);
        y += 22f;
        DrawAttr(font, x, y, "CAR", mods.Carisma);
    }

    private void DrawAttr(Font font, float x, float y, string label, int mod)
    {
        var value = CharacterCreationCatalog.BaseAttribute + mod;
        var color = mod > 0 ? new Color("5ecf5e") : mod < 0 ? new Color("e05a5a") : TextPrimary;
        HudTextDraw.AtTopLeft(this, font, new Vector2(x, y), $"{label}  {value}", 11, color);
    }

    private void DrawBorder(Rect2 rect, Color color)
    {
        const float t = 1f;
        DrawRect(new Rect2(rect.Position, new Vector2(rect.Size.X, t)), color);
        DrawRect(new Rect2(rect.Position + new Vector2(0, rect.Size.Y - t), new Vector2(rect.Size.X, t)), color);
        DrawRect(new Rect2(rect.Position, new Vector2(t, rect.Size.Y)), color);
        DrawRect(new Rect2(rect.Position + new Vector2(rect.Size.X - t, 0), new Vector2(t, rect.Size.Y)), color);
    }
}
