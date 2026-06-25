using Argentum.Client.Audio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Argentum.Client.Core;
using Argentum.Client.Models;
using Argentum.Client.Network;
using Argentum.Client.Resources;
using Argentum.Client.Validation;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>VB6 g_game_state para pantallas de login.</summary>
public enum LoginGameState
{
    ConnectScreen,
    AccountScreen,
    CreateCharScreen,
}

public partial class LoginFlow : Control
{
    public event Action<WorldSession, FrameConnection>? WorldEntered;
    public event Action<string>? StatusChanged;

    private readonly ClientStateMachine _state;
    private readonly GameResources? _resources;
    private readonly WorldView? _worldView;
    private readonly AoAudio? _audio;

    private LoginSession? _session;
    private IReadOnlyList<CharacterSummary> _characters = Array.Empty<CharacterSummary>();
    private CharacterCreationCatalog? _creation;
    private InterfaceLoader? _interface;

    private LoginGameState _gameState = LoginGameState.ConnectScreen;
    private WorldSession? _previewSession;
    private int _selectedSlot;
    private bool _rememberAccount;
    private string _accountEmail = "";

    private const int GrhCharactersScreenUi = 3839;
    private const int GrhConnectFrame = 1169;

    // FrmLogear / frmNewAccount
    private Vb6FormShell _loginShell = null!;
    private Vb6FormShell? _newAccountShell;
    private LineEdit _emailField = null!;
    private LineEdit _passwordField = null!;
    private LineEdit? _newAccountEmail;
    private LineEdit? _newAccountPassword;
    private LineEdit? _newAccountName;
    private LineEdit? _newAccountSurname;
    private LineEdit? _newAccountCaptcha;
    private Label? _captchaLabel;
    private int _captchaAnswer;
    private Button? _btnPlay;
    private Button? _btnCreateChar;
    private Button? _btnLogout;
    private Button? _btnClose;
    private CreateCharacterScreen? _createCharScreen;
    private Label _statusLabel = null!;
    private Control _connectingOverlay = null!;
    private LineEdit? _serverField;
    private bool _createInFlight;

    public LoginFlow(ClientStateMachine state, GameResources? resources, WorldView? worldView, AoAudio? audio = null)
    {
        _state = state;
        _resources = resources;
        _worldView = worldView;
        _audio = audio;
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        TextureFilter = TextureFilterEnum.Nearest;

        var resourcesRoot = ResourcesRoot.Resolve();
        if (!string.IsNullOrWhiteSpace(resourcesRoot))
        {
            _interface = new InterfaceLoader(resourcesRoot);
            _creation = CharacterCreationCatalog.Load(resourcesRoot);
        }

        _statusLabel = new Label
        {
            AnchorLeft = 0,
            AnchorTop = 1,
            AnchorRight = 1,
            AnchorBottom = 1,
            OffsetTop = -28,
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(1, 1, 1, 0.85f),
            TextureFilter = TextureFilterEnum.Linear,
        };
        AddChild(_statusLabel);

        _connectingOverlay = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.55f),
            AnchorRight = 1,
            AnchorBottom = 1,
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _connectingOverlay.AddChild(new Label
        {
            Text = "Conectando…",
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        AddChild(_connectingOverlay);

        BuildLoginShell();
        BuildAccountButtons();
        BuildCreateCharacterScreen();

        GetViewport().SizeChanged += OnViewportResized;
        CallDeferred(MethodName.LayoutVb6Ui);

        SetStatus("Iniciando…");
        _ = ConnectOnStartupAsync();
    }

    public override void _Draw()
    {
        var scale = new AoUiScale(GetViewportRect().Size);

        if (_gameState == LoginGameState.ConnectScreen)
        {
            DrawConnectScreenChrome(scale);
        }
        else if (_gameState == LoginGameState.AccountScreen)
        {
            DrawAccountScreenChrome(scale);
            if (!string.IsNullOrEmpty(_accountEmail))
            {
                var emailPos = scale.MapPoint(860, 38);
                DrawPanel(new Rect2(scale.MapPoint(734, 20), scale.MapSize(252, 32)),
                    new Color(0.04f, 0.07f, 0.11f, 0.76f), new Color(0.72f, 0.67f, 0.52f, 0.82f), 10);
                DrawCenteredUiText(_accountEmail, emailPos, 10, Colors.White);
            }
            DrawAccountCharacters(scale);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (_gameState == LoginGameState.CreateCharScreen)
        {
            return;
        }
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            return;
        }
        var scale = new AoUiScale(GetViewportRect().Size);
        var pos = GetViewport().GetMousePosition();
        if (_gameState == LoginGameState.AccountScreen && HandleAccountClick(scale, pos))
        {
            AcceptEvent();
        }
    }

    private void BuildCreateCharacterScreen()
    {
        _createCharScreen = new CreateCharacterScreen();
        _createCharScreen.BackRequested += () =>
        {
            _audio?.PlayUi(AoSoundIndex.Click);
            ShowAccountScreen();
        };
        _createCharScreen.CreateRequested += () =>
        {
            _audio?.PlayUi(AoSoundIndex.Click);
            _ = ConfirmCreateCharacterAsync();
        };
        _createCharScreen.SelectionChanged += UpdateCreateCharMapPreview;
        AddChild(_createCharScreen);
        if (_creation is not null)
        {
            _createCharScreen.Configure(_creation, _resources);
        }
    }

    private void UpdateCreateCharMapPreview()
    {
        if (_createCharScreen is null || _worldView is null || _gameState != LoginGameState.CreateCharScreen)
        {
            return;
        }
        var theme = CreateCharacterCityTheme.ForId(_createCharScreen.PreviewHomeId);
        ShowMapPreview(theme.MapId, theme.SpawnX, theme.SpawnY);
        if (_previewSession is null)
        {
            return;
        }
        _previewSession.Body = _createCharScreen.PreviewBodyId;
        _previewSession.Head = _createCharScreen.PreviewHeadId;
        _previewSession.Heading = _createCharScreen.PreviewHeading;
        _previewSession.CharacterName = _createCharScreen.CharacterName;
        _worldView.Bind(_previewSession, _resources);
        SyncWorldPreviewLayout();
    }

    private void BuildLoginShell()
    {
        _loginShell = new Vb6FormShell();
        _loginShell.SetDesignSize(464, 332);
        _loginShell.SetPanelStyle();
        AddChild(_loginShell);

        var title = _loginShell.AddCaption(UiPx(24), UiPx(18), UiPx(260), UiPx(24), "ARGENTUM ONLINE", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 18);
        var subtitle = _loginShell.AddCaption(UiPx(24), UiPx(44), UiPx(360), UiPx(18), "Ingresá con tu cuenta para continuar", new Color(0.82f, 0.85f, 0.9f));
        subtitle.AddThemeFontSizeOverride("font_size", 10);

        _loginShell.AddCaption(UiPx(24), UiPx(88), UiPx(120), UiPx(16), "Email", new Color(0.93f, 0.85f, 0.7f));
        _emailField = _loginShell.AddField(UiPx(24), UiPx(106), UiPx(416), UiPx(34), maxLength: 100);
        _emailField.PlaceholderText = "mail@servidor.com";
        _emailField.Text = System.Environment.GetEnvironmentVariable("AO_EMAIL") ?? "";

        _loginShell.AddCaption(UiPx(24), UiPx(152), UiPx(120), UiPx(16), "Contraseña", new Color(0.93f, 0.85f, 0.7f));
        _passwordField = _loginShell.AddField(UiPx(24), UiPx(170), UiPx(416), UiPx(34), secret: true, maxLength: 30);
        _passwordField.PlaceholderText = "••••••••";
        _passwordField.Text = System.Environment.GetEnvironmentVariable("AO_PASSWORD") ?? "";

        _loginShell.AddTextCheckbox(UiPx(24), UiPx(214), UiPx(160), UiPx(20), "Recordar cuenta", on => _rememberAccount = on);
        _loginShell.AddTextButton(UiPx(24), UiPx(254), UiPx(126), UiPx(38), "Ingresar", () => _ = LoginAsync(createAccount: false), primary: true);
        _loginShell.AddTextButton(UiPx(159), UiPx(254), UiPx(144), UiPx(38), "Crear cuenta", ShowNewAccount);
        _loginShell.AddTextButton(UiPx(312), UiPx(254), UiPx(128), UiPx(38), "Salir", () => GetTree().Quit());

        if (System.Environment.GetEnvironmentVariable("AO_DEV_SERVER") == "1")
        {
            _loginShell.AddCaption(UiPx(24), UiPx(304), UiPx(90), UiPx(16), "Servidor", new Color(0.76f, 0.79f, 0.84f));
            _serverField = _loginShell.AddField(UiPx(96), UiPx(296), UiPx(344), UiPx(28), maxLength: 64);
            _serverField.Text = System.Environment.GetEnvironmentVariable("AO_SERVER") ?? "127.0.0.1:7667";
        }

        _emailField.TextSubmitted += _email => { _ = LoginAsync(createAccount: false); };
        _passwordField.TextSubmitted += _password => { _ = LoginAsync(createAccount: false); };
    }

    private void BuildAccountButtons()
    {
        _btnPlay = CreateTextScreenButton("Jugar", () => _ = PlaySelectedAsync(), primary: true);
        _btnCreateChar = CreateTextScreenButton("Crear personaje", BeginCreateCharacter);
        _btnLogout = CreateTextScreenButton("Cambiar cuenta", () => _ = LogoutAsync());
        _btnClose = CreateTextScreenButton("Salir", () => GetTree().Quit());
        SetAccountButtonsVisible(false);
    }

    private Button CreateTextScreenButton(string text, Action onPress, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            Visible = false,
            FocusMode = FocusModeEnum.None,
        };
        Vb6FormShell.StyleButton(button, primary);
        button.Pressed += () =>
        {
            _audio?.PlayUi(AoSoundIndex.Click);
            onPress();
        };
        AddChild(button);
        return button;
    }

    private void SetAccountButtonsVisible(bool visible)
    {
        SetButtonVisible(_btnPlay, visible);
        SetButtonVisible(_btnCreateChar, visible);
        SetButtonVisible(_btnLogout, visible);
        SetButtonVisible(_btnClose, visible);
    }

    private static void SetButtonVisible(Button? button, bool visible)
    {
        if (button is null)
        {
            return;
        }
        button.Visible = visible;
        button.MouseFilter = visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
    }

    private void OnViewportResized() => LayoutVb6Ui();

    private void LayoutVb6Ui()
    {
        var scale = new AoUiScale(GetViewportRect().Size);
        PlaceForm(_loginShell, (int)_loginShell.DesignSize.X, (int)_loginShell.DesignSize.Y);
        if (_newAccountShell is not null)
        {
            PlaceForm(_newAccountShell, (int)_newAccountShell.DesignSize.X, (int)_newAccountShell.DesignSize.Y);
        }
        PlaceScreenButton(_btnPlay, scale, 836, 690, 128, 34);
        PlaceScreenButton(_btnCreateChar, scale, 640, 690, 180, 34);
        PlaceScreenButton(_btnLogout, scale, 24, 20, 160, 34);
        PlaceScreenButton(_btnClose, scale, 866, 20, 132, 34);
        SyncWorldPreviewLayout();
        QueueRedraw();
    }

    private void SyncWorldPreviewLayout()
    {
        if (_worldView is null)
        {
            return;
        }
        if (_gameState == LoginGameState.CreateCharScreen)
        {
            _worldView.Position = Vector2.Zero;
            _worldView.SetLoginPreview(true, GameViewport.LogicalSize, Vector2.Zero);
            return;
        }
        var viewport = GetViewportRect().Size;
        var usePreview = _worldView.Visible &&
            _gameState is LoginGameState.ConnectScreen or LoginGameState.AccountScreen;
        _worldView.Position = Vector2.Zero;
        _worldView.SetLoginPreview(usePreview, viewport, Vector2.Zero);
    }

    private void PlaceForm(Vb6FormShell form, int designWidth, int designHeight)
    {
        var scale = new AoUiScale(GetViewportRect().Size);
        var design = Vb6FormCoords.CenteredOnConnect(designWidth, designHeight);
        var rect = scale.MapRect(design.Position.X, design.Position.Y, design.Size.X, design.Size.Y);
        form.Position = rect.Position;
        form.ApplyStretch(scale);
    }

    private static void PlaceScreenButton(Button? button, AoUiScale scale, float x, float y, float width, float height)
    {
        if (button is null)
        {
            return;
        }
        var rect = scale.MapRect(x, y, width, height);
        button.Position = rect.Position;
        button.Size = rect.Size;
    }

    private void UpdateShellInputFilters()
    {
        var loginActive = _gameState == LoginGameState.ConnectScreen && _loginShell.Visible && _newAccountShell is null;
        _loginShell.MouseFilter = loginActive ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        if (_newAccountShell is not null)
        {
            _newAccountShell.MouseFilter = MouseFilterEnum.Stop;
        }
        MouseFilter = _gameState == LoginGameState.CreateCharScreen
            ? MouseFilterEnum.Ignore
            : MouseFilterEnum.Stop;
        if (_createCharScreen is not null)
        {
            _createCharScreen.MouseFilter = _gameState == LoginGameState.CreateCharScreen
                ? MouseFilterEnum.Stop
                : MouseFilterEnum.Ignore;
        }
    }

    private void ShowConnectScreen()
    {
        _gameState = LoginGameState.ConnectScreen;
        _loginShell.Visible = true;
        _newAccountShell?.QueueFree();
        _newAccountShell = null;
        _newAccountName = null;
        _newAccountSurname = null;
        _newAccountEmail = null;
        _newAccountPassword = null;
        _newAccountCaptcha = null;
        _captchaLabel = null;
        if (_createCharScreen is not null)
        {
            _createCharScreen.Visible = false;
        }
        SetAccountButtonsVisible(false);
        ShowMapPreview(1, 45, 43);
        UpdateShellInputFilters();
        LayoutVb6Ui();
        QueueRedraw();
    }

    private void ShowAccountScreen()
    {
        _gameState = LoginGameState.AccountScreen;
        _loginShell.Visible = false;
        _newAccountShell?.QueueFree();
        _newAccountShell = null;
        _newAccountName = null;
        _newAccountSurname = null;
        _newAccountEmail = null;
        _newAccountPassword = null;
        _newAccountCaptcha = null;
        _captchaLabel = null;
        if (_createCharScreen is not null)
        {
            _createCharScreen.Visible = false;
        }
        SelectFirstCharacterSlot();
        SetAccountButtonsVisible(true);
        ShowMapPreview(307, 57, 45);
        UpdateShellInputFilters();
        LayoutVb6Ui();
        QueueRedraw();
    }

    private void ShowCreateCharacterScreen()
    {
        _gameState = LoginGameState.CreateCharScreen;
        _loginShell.Visible = false;
        _newAccountShell?.QueueFree();
        _newAccountShell = null;
        HideMapPreview();
        SetAccountButtonsVisible(false);
        if (_creation is not null)
        {
            _createCharScreen?.Configure(_creation, _resources);
        }
        _createCharScreen?.Reset();
        _createCharScreen!.Visible = true;
        _createCharScreen.SetMessage(null);
        _createCharScreen.SetCreateEnabled(true);
        _createCharScreen.MoveToFront();
        _statusLabel.MoveToFront();
        UpdateCreateCharMapPreview();
        UpdateShellInputFilters();
        LayoutVb6Ui();
    }

    private void ShowNewAccount()
    {
        _newAccountShell?.QueueFree();
        _newAccountShell = new Vb6FormShell();
        _newAccountShell.SetDesignSize(560, 344);
        _newAccountShell.SetPanelStyle(new Color(0.05f, 0.08f, 0.12f, 0.88f), new Color(0.78f, 0.68f, 0.5f, 0.9f));
        AddChild(_newAccountShell);

        var title = _newAccountShell.AddCaption(UiPx(24), UiPx(18), UiPx(280), UiPx(24), "Crear cuenta", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 18);
        var subtitle = _newAccountShell.AddCaption(UiPx(24), UiPx(44), UiPx(420), UiPx(16), "Completá los datos básicos para generar la cuenta", new Color(0.82f, 0.85f, 0.9f));
        subtitle.AddThemeFontSizeOverride("font_size", 10);

        _newAccountShell.AddCaption(UiPx(24), UiPx(88), UiPx(120), UiPx(16), "Nombre", new Color(0.93f, 0.85f, 0.7f));
        _newAccountName = _newAccountShell.AddField(UiPx(24), UiPx(106), UiPx(240), UiPx(34));
        _newAccountShell.AddCaption(UiPx(296), UiPx(88), UiPx(120), UiPx(16), "Apellido", new Color(0.93f, 0.85f, 0.7f));
        _newAccountSurname = _newAccountShell.AddField(UiPx(296), UiPx(106), UiPx(240), UiPx(34));

        _newAccountShell.AddCaption(UiPx(24), UiPx(152), UiPx(120), UiPx(16), "Email", new Color(0.93f, 0.85f, 0.7f));
        _newAccountEmail = _newAccountShell.AddField(UiPx(24), UiPx(170), UiPx(240), UiPx(34));
        _newAccountShell.AddCaption(UiPx(296), UiPx(152), UiPx(120), UiPx(16), "Contraseña", new Color(0.93f, 0.85f, 0.7f));
        _newAccountPassword = _newAccountShell.AddField(UiPx(296), UiPx(170), UiPx(240), UiPx(34), secret: true);

        _newAccountShell.AddCaption(UiPx(24), UiPx(218), UiPx(120), UiPx(16), "Captcha", new Color(0.93f, 0.85f, 0.7f));
        _captchaLabel = _newAccountShell.AddCaption(UiPx(24), UiPx(240), UiPx(80), UiPx(28), "", new Color(0.95f, 0.82f, 0.56f));
        _captchaLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _captchaLabel.VerticalAlignment = VerticalAlignment.Center;
        _captchaLabel.AddThemeFontSizeOverride("font_size", 14);
        _newAccountCaptcha = _newAccountShell.AddField(UiPx(116), UiPx(236), UiPx(160), UiPx(34), maxLength: 4);
        _newAccountCaptcha.PlaceholderText = "Resultado";
        RollCaptcha();

        _newAccountShell.AddTextButton(UiPx(24), UiPx(286), UiPx(150), UiPx(38), "Cancelar", ShowConnectScreen);
        _newAccountShell.AddTextButton(UiPx(386), UiPx(286), UiPx(150), UiPx(38), "Crear cuenta", () => _ = SubmitNewAccountAsync(), primary: true);

        _loginShell.Visible = false;
        UpdateShellInputFilters();
        LayoutVb6Ui();
    }

    private void RollCaptcha()
    {
        var rng = new Random();
        var a = rng.Next(0, 10);
        var b = rng.Next(0, 10);
        _captchaAnswer = a + b;
        if (_captchaLabel is not null)
        {
            _captchaLabel.Text = $"{a} + {b}";
        }
        _newAccountCaptcha?.Clear();
    }

    private async Task SubmitNewAccountAsync()
    {
        if (_newAccountCaptcha is null || int.TryParse(_newAccountCaptcha.Text.Trim(), out var answer) == false || answer != _captchaAnswer)
        {
            SetStatus("Captcha incorrecto.");
            RollCaptcha();
            return;
        }
        _emailField.Text = _newAccountEmail?.Text.Trim() ?? "";
        _passwordField.Text = _newAccountPassword?.Text ?? "";
        _newAccountShell?.QueueFree();
        _newAccountShell = null;
        _loginShell.Visible = true;
        await LoginAsync(createAccount: true);
    }

    private void DrawAccountCharacters(AoUiScale scale)
    {
        const int sumax = 84;
        for (var slot = 1; slot <= CharacterCreationCatalog.MaxCharactersPerAccount; slot++)
        {
            float x;
            float y;
            if (slot > 5)
            {
                x = slot * 132f - 5 * 132f + sumax;
                y = 440;
            }
            else
            {
                x = slot * 132f + sumax;
                y = 283;
            }
            var summary = SlotCharacter(slot);
            if (summary is null)
            {
                continue;
            }
            if (_resources is not null)
            {
                LoginPortraitDrawer.DrawCharacterSlot(
                    this, _resources, summary.Body, summary.Head, 3, scale, x, y);
            }
            var namePos = scale.MapPoint(x + 20, y + 58);
            var nameWidth = AoUiFonts.Ui.GetStringSize(summary.Name, fontSize: 10).X;
            DrawString(AoUiFonts.Ui, (namePos - new Vector2(nameWidth / 2f, 0)).Round(),
                summary.Name, fontSize: 10, modulate: Colors.White);
            if (slot == _selectedSlot)
            {
                var titlePos = scale.MapPoint(511, 565);
                DrawCenteredUiText(summary.Name, titlePos, 11, Colors.White);

                var line1 = $"{CharacterClassLabel(summary)} · Nv {summary.Level}";
                var line2 = CharacterLocationLabel(summary);
                var line3 = CompactEquipmentLine(summary);

                DrawCenteredUiText(line1, titlePos + new Vector2(0, 16f), 9, new Color(0.95f, 0.9f, 0.72f));
                DrawCenteredUiText(line2, titlePos + new Vector2(0, 29f), 9, new Color(0.82f, 0.86f, 0.95f));
                if (!string.IsNullOrWhiteSpace(line3))
                {
                    DrawCenteredUiText(line3, titlePos + new Vector2(0, 42f), 8, new Color(0.8f, 0.8f, 0.8f));
                }
            }
        }
    }

    private void DrawConnectScreenChrome(AoUiScale scale)
    {
        DrawPanel(new Rect2(scale.MapPoint(20, 20), scale.MapSize(260, 72)),
            new Color(0.04f, 0.07f, 0.11f, 0.76f), new Color(0.74f, 0.68f, 0.52f, 0.82f), 12);
        DrawString(AoUiFonts.Ui, scale.MapPoint(36, 48).Round(), "ARGENTUM ONLINE", fontSize: 16, modulate: Colors.White);
        DrawString(AoUiFonts.Ui, scale.MapPoint(36, 68).Round(), "Mundo vivo al fondo, interfaz limpia al frente", fontSize: 9,
            modulate: new Color(0.82f, 0.85f, 0.9f));
    }

    private void DrawAccountScreenChrome(AoUiScale scale)
    {
        DrawPanel(new Rect2(scale.MapPoint(20, 20), scale.MapSize(240, 72)),
            new Color(0.04f, 0.07f, 0.11f, 0.76f), new Color(0.74f, 0.68f, 0.52f, 0.82f), 12);
        DrawString(AoUiFonts.Ui, scale.MapPoint(36, 48).Round(), "SELECCIONAR PERSONAJE", fontSize: 16, modulate: Colors.White);
        DrawString(AoUiFonts.Ui, scale.MapPoint(36, 68).Round(), "Elegí un personaje o creá uno nuevo", fontSize: 9,
            modulate: new Color(0.82f, 0.85f, 0.9f));

        for (var slot = 1; slot <= CharacterCreationCatalog.MaxCharactersPerAccount; slot++)
        {
            var summary = SlotCharacter(slot);
            var rect = AccountSlotPanelRect(scale, slot);
            var fill = slot == _selectedSlot
                ? new Color(0.11f, 0.15f, 0.2f, 0.84f)
                : new Color(0.05f, 0.07f, 0.1f, 0.72f);
            var border = slot == _selectedSlot
                ? new Color(0.95f, 0.82f, 0.56f, 0.96f)
                : new Color(0.62f, 0.64f, 0.68f, 0.48f);
            DrawPanel(rect, fill, border, 10);
            if (summary is null)
            {
                DrawCenteredUiText("Vacío", rect.GetCenter() + new Vector2(0, 6f), 9, new Color(0.68f, 0.72f, 0.78f));
            }
        }

        DrawPanel(new Rect2(scale.MapPoint(338, 540), scale.MapSize(348, 84)),
            new Color(0.04f, 0.07f, 0.11f, 0.78f), new Color(0.74f, 0.68f, 0.52f, 0.85f), 12);
    }

    private Rect2 AccountSlotPanelRect(AoUiScale scale, int slot)
    {
        const int sumax = 84;
        float x;
        float y;
        if (slot > 5)
        {
            x = slot * 132f - 5 * 132f + sumax;
            y = 440;
        }
        else
        {
            x = slot * 132f + sumax;
            y = 283;
        }
        return scale.MapRect(x - 10f, y - 18f, 92f, 108f);
    }

    private void DrawPanel(Rect2 rect, Color fill, Color border, int radius)
    {
        var box = new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            ShadowColor = new Color(0, 0, 0, 0.22f),
            ShadowSize = 5,
        };
        DrawStyleBox(box, rect);
    }

    private bool HandleAccountClick(AoUiScale scale, Vector2 pos)
    {
        if (TryPickCharacterSlot(scale, pos, out var slot))
        {
            _selectedSlot = slot;
            UpdateAccountPreview(slot);
            QueueRedraw();
            return true;
        }
        return false;
    }

    private bool TryPickCharacterSlot(AoUiScale scale, Vector2 pos, out int slot)
    {
        slot = 0;
        for (var i = 1; i <= CharacterCreationCatalog.MaxCharactersPerAccount; i++)
        {
            var col = (i - 1) % 5;
            var row = (i - 1) / 5;
            var rect = new Rect2(207 + col * 131, 246 + row * 158, 79, 93);
            if (!scale.Hit(rect, pos))
            {
                continue;
            }
            if (SlotCharacter(i) is null)
            {
                return false;
            }
            slot = i;
            return true;
        }
        return false;
    }

    private CharacterSummary? SlotCharacter(int slot)
    {
        if (slot <= 0 || slot > _characters.Count)
        {
            return null;
        }
        return _characters[slot - 1];
    }

    private void SelectFirstCharacterSlot()
    {
        _selectedSlot = 0;
        for (var slot = 1; slot <= CharacterCreationCatalog.MaxCharactersPerAccount; slot++)
        {
            if (SlotCharacter(slot) is null)
            {
                continue;
            }
            _selectedSlot = slot;
            UpdateAccountPreview(slot);
            return;
        }
        ShowMapPreview(307, 57, 45);
    }

    private string ClassLabel(int classId)
    {
        if (_creation is null || classId <= 0 || classId >= _creation.Classes.Count)
        {
            return string.Empty;
        }
        return _creation.Classes[classId];
    }

    private string CharacterClassLabel(CharacterSummary summary)
    {
        var className = ClassLabel(summary.Class);
        return string.IsNullOrWhiteSpace(className) ? $"Clase {summary.Class}" : className;
    }

    private static string CharacterLocationLabel(CharacterSummary summary) =>
        $"Mapa {summary.Map} · {summary.PosX}, {summary.PosY}";

    private string CompactEquipmentLine(CharacterSummary summary)
    {
        var parts = new List<string>(4);
        AppendEquipmentPart(parts, "Arma", summary.Weapon, "—");
        AppendEquipmentPart(parts, "Esc", summary.Shield, "—");
        AppendEquipmentPart(parts, "Casco", summary.Helmet, "—");
        AppendEquipmentPart(parts, "Moch", summary.Backpack, "—");
        return string.Join("  ·  ", parts);
    }

    private void AppendEquipmentPart(List<string> parts, string label, int itemId, string emptyLabel)
    {
        parts.Add($"{label} {VisibleEquipmentName(itemId, emptyLabel)}");
    }

    private void DrawCenteredUiText(string text, Vector2 designPos, int fontSize, Color color)
    {
        var screenPos = designPos.Round();
        var width = AoUiFonts.Ui.GetStringSize(text, fontSize: fontSize).X;
        DrawString(AoUiFonts.Ui, (screenPos - new Vector2(width / 2f, 0)).Round(),
            text, fontSize: fontSize, modulate: color);
    }

    private static int UiPx(float pixels) => (int)MathF.Round(pixels * Vb6FormCoords.TwipsPerPixel);

    private string VisibleEquipmentName(int itemId, string emptyLabel)
    {
        if (itemId <= 0)
        {
            return emptyLabel;
        }

        var itemName = _resources?.Items?.Get(itemId)?.Name;
        if (!string.IsNullOrWhiteSpace(itemName))
        {
            return ShortenUiText(itemName, 12);
        }

        return $"Obj {itemId}";
    }

    private static string ShortenUiText(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxChars)
        {
            return text;
        }

        return text[..(maxChars - 1)].TrimEnd() + "…";
    }

    private void BeginCreateCharacter()
    {
        if (_characters.Count >= CharacterCreationCatalog.MaxCharactersPerAccount)
        {
            SetStatus("Llegaste al límite de 10 personajes por cuenta.");
            return;
        }
        ShowCreateCharacterScreen();
    }

    private void UpdateAccountPreview(int slot)
    {
        var character = SlotCharacter(slot);
        if (character is null)
        {
            ShowMapPreview(307, 57, 45);
            ClearPreviewCharacter();
            return;
        }
        ShowMapPreview(character.Map, character.PosX, character.PosY);
        // VB6 RenderAccountCharacters: el mapa es fondo; los PJs solo se dibujan en los slots.
        ClearPreviewCharacter();
    }

    private void ClearPreviewCharacter()
    {
        if (_previewSession is null)
        {
            return;
        }
        _previewSession.Body = 0;
        _previewSession.Head = 0;
        _previewSession.CharacterName = string.Empty;
        _worldView?.Bind(_previewSession, _resources);
    }

    private void ShowMapPreview(int mapId, int x, int y)
    {
        if (_worldView is null)
        {
            return;
        }
        _previewSession ??= new WorldSession();
        _previewSession.MapsWorld = _resources?.MapsWorld;
        _previewSession.SetMap(mapId);
        _previewSession.SnapPosition(x, y);
        _previewSession.LoggedIn = true;
        _worldView.Visible = true;
        _worldView.Bind(_previewSession, _resources);
        SyncWorldPreviewLayout();
    }

    private void HideMapPreview()
    {
        if (_worldView is not null)
        {
            _worldView.Visible = false;
            _worldView.SetLoginPreview(false, Vector2.Zero);
        }
    }

    private async Task ConnectOnStartupAsync()
    {
        var server = _serverField?.Text ?? System.Environment.GetEnvironmentVariable("AO_SERVER") ?? "127.0.0.1:7667";
        await ConnectAsync(server);
    }

    private async Task ConnectAsync(string address)
    {
        try
        {
            SetConnecting(true);
            SetStatus("Conectando…");
            if (_state.Current == ClientState.Boot)
            {
                _state.Transition(ClientState.Connecting);
            }
            var (host, port) = ParseServer(address);
            if (_session is not null)
            {
                await _session.DisposeAsync();
            }
            _session = new LoginSession();
            await _session.ConnectAsync(host, port);
            _state.Transition(ClientState.Account);
            SetStatus("Conectado.");
            ShowConnectScreen();
        }
        catch (Exception ex)
        {
            SetStatus($"Error de conexión: {ex.Message}");
        }
        finally
        {
            SetConnecting(false);
        }
    }

    private async Task LoginAsync(bool createAccount)
    {
        try
        {
            if (_session is null || !_session.IsConnected)
            {
                SetStatus("No hay conexión al servidor.");
                return;
            }
            if (!CharValidation.CheckAccountLogin(_emailField.Text.Trim(), _passwordField.Text, out var error))
            {
                SetStatus(error);
                return;
            }
            SetConnecting(true);
            SetStatus(createAccount ? "Creando cuenta…" : "Iniciando sesión…");
            var email = _emailField.Text.Trim();
            _characters = createAccount
                ? await _session.CreateAccountAsync(email, _passwordField.Text)
                : await _session.LoginAccountAsync(email, _passwordField.Text);
            _accountEmail = email;
            _state.Transition(ClientState.Characters);
            SetStatus($"{_characters.Count} personaje(s).");
            ShowAccountScreen();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
            _loginShell.Visible = true;
        }
        finally
        {
            SetConnecting(false);
        }
    }

    private async Task PlaySelectedAsync()
    {
        var character = SlotCharacter(_selectedSlot);
        if (character is null)
        {
            SetStatus("Seleccioná un personaje.");
            return;
        }
        try
        {
            SetConnecting(true);
            SetStatus($"Entrando como {character.Name}…");
            await _session!.EnterExistingCharacterAsync(character);
            await FinishWorldEntryAsync();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
        finally
        {
            SetConnecting(false);
        }
    }

    private async Task ConfirmCreateCharacterAsync()
    {
        if (_createCharScreen is null || _createInFlight)
        {
            return;
        }
        var name = _createCharScreen.CharacterName;
        if (!CharValidation.TryValidateCharacterName(name, out var error))
        {
            _createCharScreen.SetMessage(error);
            SetStatus(error);
            return;
        }
        if (!_createCharScreen.TryGetSelection(out var race, out var gender, out var classId, out var head, out var home))
        {
            const string msg = "Completá raza, género, clase, cabeza y hogar.";
            _createCharScreen.SetMessage(msg);
            SetStatus(msg);
            return;
        }
        if (!await EnsureAccountSessionAsync())
        {
            const string msg = "No hay sesión activa. Volvé a la cuenta e intentá de nuevo.";
            _createCharScreen.SetMessage(msg);
            SetStatus(msg);
            return;
        }
        try
        {
            _createInFlight = true;
            _createCharScreen.SetCreateEnabled(false);
            _createCharScreen.SetMessage(null);
            SetConnecting(true);
            SetStatus("Creando personaje…");
            GD.Print($"[CrearPJ] enviando LoginNewChar name={name} race={race} gender={gender} class={classId} head={head} home={home}");
            await _session!.SendNewCharacterAsync(name, race, gender, classId, head, home);
            await FinishWorldEntryAsync();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"ConfirmCreateCharacterAsync: {ex}");
            if (!await TryRecoverAfterCreateFailureAsync())
            {
                _createCharScreen.SetMessage(ex.Message);
                SetStatus(ex.Message);
                ShowCreateCharacterScreen();
            }
        }
        finally
        {
            _createInFlight = false;
            _createCharScreen?.SetCreateEnabled(true);
            SetConnecting(false);
        }
    }

    private async Task<bool> EnsureAccountSessionAsync()
    {
        if (_session is not null && _session.IsConnected)
        {
            return true;
        }
        var email = string.IsNullOrWhiteSpace(_emailField.Text) ? _accountEmail : _emailField.Text.Trim();
        var password = _passwordField.Text;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }
        try
        {
            if (_session is not null)
            {
                await _session.DisposeAsync();
                _session = null;
            }
            var server = _serverField?.Text ?? System.Environment.GetEnvironmentVariable("AO_SERVER") ?? "127.0.0.1:7667";
            var (host, port) = ParseServer(server);
            _session = new LoginSession();
            await _session.ConnectAsync(host, port);
            _characters = await _session.LoginAccountAsync(email, password);
            _accountEmail = email;
            return _session.IsConnected;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"EnsureAccountSessionAsync: {ex}");
            return false;
        }
    }

    private async Task<bool> TryRecoverAfterCreateFailureAsync()
    {
        var email = string.IsNullOrWhiteSpace(_emailField.Text) ? _accountEmail : _emailField.Text.Trim();
        var password = _passwordField.Text;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }
        try
        {
            if (_session is not null)
            {
                await _session.DisposeAsync();
                _session = null;
            }
            var server = _serverField?.Text ?? System.Environment.GetEnvironmentVariable("AO_SERVER") ?? "127.0.0.1:7667";
            var (host, port) = ParseServer(server);
            _session = new LoginSession();
            await _session.ConnectAsync(host, port);
            _characters = await _session.LoginAccountAsync(email, password);
            _accountEmail = email;
            if (_characters.Count == 0)
            {
                return false;
            }
            SetStatus($"{_characters.Count} personaje(s). El personaje fue creado; seleccioná uno para jugar.");
            ShowAccountScreen();
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"TryRecoverAfterCreateFailureAsync: {ex}");
            return false;
        }
    }

    private async Task FinishWorldEntryAsync()
    {
        if (_session is null || !_session.IsConnected)
        {
            throw new InvalidOperationException("No hay sesión activa.");
        }

        var connection = _session.RequireConnection();
        var first = await connection.ReadFrameAsync();
        if (LoginSession.TryParseError(first, out var message))
        {
            throw new InvalidOperationException(message);
        }
        var world = await new WorldEntryReader().ReadAsync(connection, first);
        var gameplayConnection = _session.TakeConnection();
        world.MapsWorld = _resources?.MapsWorld;
        world.SetMap(world.MapId, world.MapResource);
        _state.Transition(ClientState.World);
        Visible = false;
        HideMapPreview();
        WorldEntered?.Invoke(world, gameplayConnection);
        SetStatus($"En el mundo · {world.CharacterName}");
    }

    private async Task LogoutAsync()
    {
        if (_session is not null)
        {
            await _session.DisposeAsync();
            _session = null;
        }
        _characters = Array.Empty<CharacterSummary>();
        if (_state.Current == ClientState.Characters)
        {
            _state.Transition(ClientState.Account);
        }
        var server = _serverField?.Text ?? System.Environment.GetEnvironmentVariable("AO_SERVER") ?? "127.0.0.1:7667";
        ShowConnectScreen();
        await ConnectAsync(server);
        SetStatus("Sesión cerrada.");
    }

    /// <summary>Vuelve a la pantalla de personajes tras salir del mundo.</summary>
    public async Task ReturnToCharacterSelectAsync()
    {
        try
        {
            SetConnecting(true);
            var email = string.IsNullOrWhiteSpace(_emailField.Text) ? _accountEmail : _emailField.Text.Trim();
            var password = _passwordField.Text;
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await LogoutFromAccountAsync();
                return;
            }

            if (_state.Current == ClientState.World)
            {
                _state.Transition(ClientState.Characters);
            }

            Visible = true;
            var server = _serverField?.Text ?? System.Environment.GetEnvironmentVariable("AO_SERVER") ?? "127.0.0.1:7667";
            var (host, port) = ParseServer(server);
            if (_session is not null)
            {
                await _session.DisposeAsync();
            }
            _session = new LoginSession();
            await _session.ConnectAsync(host, port);
            _characters = await _session.LoginAccountAsync(email, password);
            _accountEmail = email;
            SetStatus($"{_characters.Count} personaje(s).");
            ShowAccountScreen();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
            ShowConnectScreen();
        }
        finally
        {
            SetConnecting(false);
        }
    }

    /// <summary>Cierra sesión de cuenta (desde mundo o selección de PJs).</summary>
    public async Task LogoutFromAccountAsync()
    {
        if (_state.Current == ClientState.World)
        {
            _state.Transition(ClientState.Characters);
        }
        await LogoutAsync();
    }

    private void SetConnecting(bool visible)
    {
        _connectingOverlay.Visible = visible;
        _connectingOverlay.MouseFilter = visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
    }

    private void SetStatus(string text)
    {
        _statusLabel.Text = text;
        StatusChanged?.Invoke(text);
    }

    private static (string Host, int Port) ParseServer(string address)
    {
        var separator = address.LastIndexOf(':');
        if (separator <= 0 || !int.TryParse(address[(separator + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var port))
        {
            throw new FormatException($"Dirección inválida: {address}");
        }
        return (address[..separator], port);
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _ = _session.DisposeAsync();
        }
    }
}
