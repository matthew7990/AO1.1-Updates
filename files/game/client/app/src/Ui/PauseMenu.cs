using System;
using Argentum.Client.Core;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>Menú de pausa (Escape): salir del PJ/cuenta, resolución y cerrar juego.</summary>
public partial class PauseMenu : Control
{
    private static readonly Color Backdrop = new(0, 0, 0, 0.62f);
    private static readonly Color PanelFill = new(0.05f, 0.06f, 0.1f, 0.96f);
    private static readonly Color PanelBorder = new(0.68f, 0.58f, 0.36f, 0.75f);

    private PanelContainer _panel = null!;
    private OptionButton _resolution = null!;
    private CheckBox _windowed = null!;
    private bool _open;
    private bool _syncingSettings;

    public bool IsOpen => _open;

    public event Action? ContinuePressed;
    public event Action? CharacterSelectPressed;
    public event Action? LogoutPressed;
    public event Action? QuitPressed;
    public event Action? SettingsChanged;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var backdrop = new ColorRect
        {
            Color = Backdrop,
            MouseFilter = MouseFilterEnum.Stop,
        };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        _panel = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Stop,
        };
        _panel.SetAnchorsPreset(LayoutPreset.Center);
        _panel.OffsetLeft = -170;
        _panel.OffsetRight = 170;
        _panel.OffsetTop = -210;
        _panel.OffsetBottom = 210;
        var panelStyle = new StyleBoxFlat
        {
            BgColor = PanelFill,
            BorderColor = PanelBorder,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 16,
            ContentMarginBottom = 16,
        };
        _panel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(_panel);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        _panel.AddChild(root);

        var title = new Label
        {
            Text = "Menú",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 20);
        root.AddChild(title);

        root.AddChild(MakeButton("Continuar", () => ContinuePressed?.Invoke()));
        root.AddChild(MakeButton("Selección de personajes", () => CharacterSelectPressed?.Invoke()));
        root.AddChild(MakeButton("Cerrar sesión", () => LogoutPressed?.Invoke()));

        var settingsBox = new VBoxContainer();
        settingsBox.AddThemeConstantOverride("separation", 6);
        settingsBox.AddChild(new Label { Text = "Pantalla" });
        _resolution = new OptionButton();
        foreach (var preset in GameViewport.AvailablePresets)
        {
            _resolution.AddItem($"{preset.Name} ({preset.Width}×{preset.Height})");
        }
        _resolution.ItemSelected += _ => ApplyDisplaySettings();
        settingsBox.AddChild(_resolution);
        _windowed = new CheckBox { Text = "Modo ventana" };
        _windowed.Toggled += _ => ApplyDisplaySettings();
        settingsBox.AddChild(_windowed);
        root.AddChild(settingsBox);

        root.AddChild(MakeButton("Salir del juego", () => QuitPressed?.Invoke()));
    }

    public void Open()
    {
        SyncDisplaySettings();
        _open = true;
        Visible = true;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public void Close()
    {
        _open = false;
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Toggle()
    {
        if (_open)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    private void SyncDisplaySettings()
    {
        _syncingSettings = true;
        _resolution.Select(GameViewport.GetPresetIndex(GameViewport.Active));
        _windowed.SetPressedNoSignal(GameViewport.Windowed);
        _syncingSettings = false;
    }

    private void ApplyDisplaySettings()
    {
        if (_syncingSettings)
        {
            return;
        }

        var preset = GameViewport.AvailablePresets[_resolution.Selected];
        var windowed = _windowed.ButtonPressed;
        if (preset.Name == GameViewport.Active.Name && windowed == GameViewport.Windowed)
        {
            return;
        }

        var window = GetWindow();
        GameViewport.SetPreset(preset, window);
        GameViewport.SetWindowed(windowed, window);
        SettingsChanged?.Invoke();
    }

    private static Button MakeButton(string text, Action onPressed)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(280, 34),
        };
        button.Pressed += onPressed;
        return button;
    }
}
