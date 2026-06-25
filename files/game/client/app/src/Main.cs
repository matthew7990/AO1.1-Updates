using System;
using Argentum.Client.Audio;
using Argentum.Client.Core;
using Argentum.Client.Gameplay;
using Argentum.Client.Network;
using Argentum.Client.Resources;
using Argentum.Client.Ui;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client;

public partial class Main : Node
{
    private readonly ClientStateMachine _state = new();
    private GameplaySession? _gameplay;
    private GameResources? _resources;
    private WorldView? _worldView;
    private WorldFogOverlay? _fogOverlay;
    private SubViewportContainer? _worldHost;
    private Label? _hud;
    private Label? _fpsLabel;
    private GameplayHud? _gameplayHud;
    private GameplayConsole? _gameplayConsole;
    private GameplayChatInput? _gameplayChat;
    private CommerceScreen? _commerceScreen;
    private SpellScreen? _spellScreen;
    private FloorItemTooltip? _floorItemTooltip;
    private string? _pendingCommerceVendor;
    private bool _pendingCommerceClose;
    private double _chatSuppressOpenUntil;
    private AoAudio? _audio;
    private LoginFlow? _loginFlow;
    private PauseMenu? _pauseMenu;
    private bool _pauseActionRunning;
    private double _fpsAccum;
    private int _fpsFrames;
    private int _displayFps;
    private double _walkCooldown;
    private double _attackCooldown;
    private double _spellCooldown;
    private bool _wasWarping;
    private Vector2I _lastLayoutWindowSize;
    private int _walkSendSeq;

    public override void _Ready()
    {
        _worldView = GetNode<WorldView>("WorldLayer/WorldHost/WorldViewport/WorldView");
        _fogOverlay = GetNode<WorldFogOverlay>("WorldLayer/WorldHost/WorldViewport/WorldFogOverlay");
        _worldHost = GetNode<SubViewportContainer>("WorldLayer/WorldHost");
        _hud = GetNode<Label>("UiLayer/Hud");
        _fpsLabel = GetNode<Label>("UiLayer/FpsLabel");
        _gameplayHud = new GameplayHud();
        GetNode<CanvasLayer>("UiLayer").AddChild(_gameplayHud);
        _gameplayConsole = new GameplayConsole();
        GetNode<CanvasLayer>("UiLayer").AddChild(_gameplayConsole);
        _gameplayChat = new GameplayChatInput();
        GetNode<CanvasLayer>("UiLayer").AddChild(_gameplayChat);
        _commerceScreen = new CommerceScreen();
        GetNode<CanvasLayer>("UiLayer").AddChild(_commerceScreen);
        _spellScreen = new SpellScreen();
        GetNode<CanvasLayer>("UiLayer").AddChild(_spellScreen);
        _floorItemTooltip = new FloorItemTooltip();
        GetNode<CanvasLayer>("UiLayer").AddChild(_floorItemTooltip);
        _resources = GameResources.TryLoad();
        _audio = new AoAudio { Name = "AoAudio" };
        AddChild(_audio);
        ConfigureWindow();
        _lastLayoutWindowSize = GetWindow().Size;
        _gameplayChat.MessageSubmitted += OnChatSubmitted;

        _loginFlow = new LoginFlow(_state, _resources, _worldView, _audio);
        _loginFlow.WorldEntered += OnWorldEntered;
        _loginFlow.StatusChanged += text => _hud!.Text = text;
        GetNode<CanvasLayer>("UiLayer").AddChild(_loginFlow);

        _pauseMenu = new PauseMenu();
        _pauseMenu.ContinuePressed += () => _pauseMenu.Close();
        _pauseMenu.CharacterSelectPressed += () => _ = OnPauseCharacterSelectAsync();
        _pauseMenu.LogoutPressed += () => _ = OnPauseLogoutAsync();
        _pauseMenu.QuitPressed += () => GetTree().Quit();
        _pauseMenu.SettingsChanged += OnPauseSettingsChanged;
        GetNode<CanvasLayer>("UiLayer").AddChild(_pauseMenu);

        _hud.Text = _resources is null
            ? "AO · definí AO_RESOURCES para gráficos oficiales"
            : "AO · recursos cargados";
        _fpsLabel.Text = "FPS —";
        _worldView.Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (_gameplayHud is not null && _state.Current == ClientState.World)
        {
            var hudRect = _gameplayHud.GetGlobalRect();
            var local = Vector2.Zero;
            if (@event is InputEventMouseMotion motion)
            {
                local = motion.GlobalPosition - hudRect.Position;
            }
            else if (@event is InputEventMouseButton button)
            {
                local = button.GlobalPosition - hudRect.Position;
            }

            if (@event is not InputEventMouseButton { Pressed: true, DoubleClick: true, ButtonIndex: MouseButton.Left }
                && _gameplayHud.HandleInventoryPointerInput(@event, local))
            {
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event is InputEventKey { Pressed: true, Echo: false } hotkeyEvt
            && _state.Current == ClientState.World && CanUseSpellHotkeys())
        {
            if (hotkeyEvt.Keycode == Key.Space && TryPickHoveredFloorItem())
            {
                GetViewport().SetInputAsHandled();
                return;
            }
            var hotkey = KeyToSpellHotbar(hotkeyEvt.Keycode);
            if (hotkey > 0)
            {
                CastSpellFromHotkey(hotkey);
                GetViewport().SetInputAsHandled();
                return;
            }
        }
        if (@event is InputEventMouseButton { Pressed: true, DoubleClick: true, ButtonIndex: MouseButton.Left } dblClick
            && !dblClick.CtrlPressed && !Input.IsPhysicalKeyPressed(Key.Ctrl))
        {
            if (TryInventoryUseClick(dblClick.GlobalPosition))
            {
                GetViewport().SetInputAsHandled();
                return;
            }
        }
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click
            && !click.CtrlPressed && !Input.IsPhysicalKeyPressed(Key.Ctrl))
        {
            if (TrySpellHotbarClick(click.GlobalPosition))
            {
                GetViewport().SetInputAsHandled();
                return;
            }
        }
        if (!CanProcessWorldMouseInput())
        {
            return;
        }
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } worldClick
            && !worldClick.CtrlPressed && !Input.IsPhysicalKeyPressed(Key.Ctrl))
        {
            if (TryLeftClickFromGlobal(worldClick.GlobalPosition))
            {
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private bool CanUseSpellHotkeys() =>
        _state.Current == ClientState.World
        && !_pauseActionRunning
        && _pauseMenu?.IsOpen != true
        && _gameplay is not null
        && !_gameplay.World.IsDead
        && _gameplayChat?.IsOpen != true
        && _commerceScreen?.IsOpen != true;

    private bool CanProcessWorldMouseInput() =>
        CanUseSpellHotkeys();

    private bool TryInventoryUseClick(Vector2 globalPos)
    {
        if (_gameplayHud is null || _gameplay is null || !CanUseSpellHotkeys() || !_gameplayHud.IsInventoryOpen)
        {
            return false;
        }
        var rect = _gameplayHud.GetGlobalRect();
        if (!rect.HasPoint(globalPos))
        {
            return false;
        }
        var local = globalPos - rect.Position;
        if (!_gameplayHud.TryHitInventorySlot(local, out var slot))
        {
            return false;
        }
        var item = _gameplay.World.Inventory.GetSlot(slot);
        if (item.IsEmpty)
        {
            return false;
        }
        var def = _resources?.Items?.Get(item.ObjIndex);
        if (def != null)
        {
            if (ItemAffixes.IsEquipmentType(def.ObjType))
            {
                GD.Print($"Main: TryInventoryUseClick -> SendEquipAsync slot={slot} obj={def.Name} objType={def.ObjType}");
                _ = _gameplay.SendEquipAsync(slot);
            }
            else
            {
                GD.Print($"Main: TryInventoryUseClick -> SendUseItemAsync slot={slot} obj={def.Name} objType={def.ObjType}");
                _ = _gameplay.SendUseItemAsync(slot);
            }
        }
        else
        {
            GD.Print($"Main: TryInventoryUseClick -> SendUseItemAsync slot={slot} (no def)");
            _ = _gameplay.SendUseItemAsync(slot);
        }
        return true;
    }

    private bool TrySpellHotbarClick(Vector2 globalPos)
    {
        if (_gameplayHud is null || _gameplay is null || !CanUseSpellHotkeys())
        {
            return false;
        }
        var rect = _gameplayHud.GetGlobalRect();
        if (!rect.HasPoint(globalPos))
        {
            return false;
        }
        var local = globalPos - rect.Position;
        if (!_gameplayHud.TryHitSpellHotbar(local, out var hotkey))
        {
            return false;
        }
        if (_spellScreen?.IsOpen == true
            && _gameplay.World.Spells.GetSlot(_spellScreen.SelectedSpellBookSlot) > 0)
        {
            _gameplay.World.SpellHotbar.Assign(hotkey, _spellScreen.SelectedSpellBookSlot);
            _gameplayHud.Refresh();
            _gameplay.World.Console.Add(
                $"Hechizo asignado a [{SpellHotbar.HotkeyLabel(hotkey)}].",
                new Color("a8c8ff"));
            return true;
        }
        CastSpellFromHotkey(hotkey);
        return true;
    }

    private static int KeyToSpellHotbar(Key key) => key switch
    {
        Key.Key1 => 1,
        Key.Key2 => 2,
        Key.Key3 => 3,
        Key.Key4 => 4,
        Key.Key5 => 5,
        Key.Key6 => 6,
        Key.Key7 => 7,
        Key.Key8 => 8,
        Key.Key9 => 9,
        Key.Key0 => 10,
        _ => 0,
    };

    private void CastSpellFromHotkey(int hotkey)
    {
        if (_gameplay is null)
        {
            return;
        }
        var slot = _gameplay.World.SpellHotbar.GetSpellBookSlot(hotkey);
        if (slot <= 0)
        {
            _gameplay.World.Console.Add(
                $"Barra [{SpellHotbar.HotkeyLabel(hotkey)}] vacía. Abrí [H] y asigná un hechizo.",
                new Color("f0d878"));
            return;
        }
        if (_spellCooldown > 0)
        {
            return;
        }
        _spellScreen?.Close();
        _gameplayHud!.SpellAssignMode = false;
        _spellCooldown = _gameplay.World.MagicIntervalMs / 1000.0;
        _ = _gameplay.SendCastSpellAsync(slot);
    }

    private bool TryLeftClickFromGlobal(Vector2 globalPos)
    {
        if (_worldHost is null)
        {
            return false;
        }
        if (!GameViewport.TryMapGlobalMouseToLogical(_worldHost, globalPos, out var logical))
        {
            return false;
        }
        return TryLeftClick(logical);
    }

    private void OnChatSubmitted(string message)
    {
        if (_gameplay is null)
        {
            return;
        }
        var name = _gameplay.World.CharacterName;
        if (!string.IsNullOrWhiteSpace(name))
        {
            _gameplay.World.Dialogs.Set(_gameplay.World.CharIndex, name, message, new Color("f0ece4"));
            _gameplay.World.Console.Add($"[{name}] {message}", new Color("7ab8ff"));
        }
        _chatSuppressOpenUntil = Time.GetTicksMsec() + 350;
        _ = _gameplay.SendTalkAsync(message);
    }

    private void ConfigureWindow()
    {
        var window = GetWindow();
        var worldViewport = GetNode<SubViewport>("WorldLayer/WorldHost/WorldViewport");
        var worldHost = GetNode<SubViewportContainer>("WorldLayer/WorldHost");
        GameViewport.Configure(window, worldViewport, worldHost);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusIn || what == NotificationWMSizeChanged)
        {
            GameViewport.ScheduleRestoreLayout(GetWindow());
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_state.Current != ClientState.World || _pauseActionRunning)
        {
            return;
        }
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            _pauseMenu?.Toggle();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (_gameplay is null || _pauseMenu?.IsOpen == true)
        {
            return;
        }
        if (_gameplayChat?.IsOpen == true)
        {
            return;
        }
        if (_commerceScreen?.IsOpen == true)
        {
            return;
        }
        if (Time.GetTicksMsec() < _chatSuppressOpenUntil)
        {
            return;
        }
        if (@event is InputEventKey { Pressed: true, Echo: false } key && key.Keycode == Key.I)
        {
            _gameplayHud?.ToggleInventory();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event is InputEventKey { Pressed: true, Echo: false } characterKey && characterKey.Keycode == Key.C)
        {
            _gameplayHud?.ToggleCharacterPanel();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event is InputEventKey { Pressed: false, Echo: false } enterKey
            && (enterKey.Keycode == Key.Enter || enterKey.Keycode == Key.KpEnter))
        {
            _gameplayChat!.Open();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnPauseSettingsChanged()
    {
        GameViewport.ScheduleRestoreLayout(GetWindow());
        _gameplayHud?.Refresh();
        CallDeferred(MethodName.RedrawWorld);
    }

    private async System.Threading.Tasks.Task OnPauseCharacterSelectAsync()
    {
        if (_pauseActionRunning)
        {
            return;
        }
        _pauseActionRunning = true;
        _pauseMenu?.Close();
        try
        {
            await ExitWorldAsync();
            await _loginFlow!.ReturnToCharacterSelectAsync();
        }
        finally
        {
            _pauseActionRunning = false;
        }
    }

    private async System.Threading.Tasks.Task OnPauseLogoutAsync()
    {
        if (_pauseActionRunning)
        {
            return;
        }
        _pauseActionRunning = true;
        _pauseMenu?.Close();
        try
        {
            await ExitWorldAsync();
            await _loginFlow!.LogoutFromAccountAsync();
        }
        finally
        {
            _pauseActionRunning = false;
        }
    }

    private async System.Threading.Tasks.Task ExitWorldAsync()
    {
        if (_gameplay is not null)
        {
            await _gameplay.CloseAsync();
            _gameplay = null;
        }
        _worldView!.Visible = false;
        _gameplayHud!.Bind(null, null);
        _gameplayConsole?.Bind(null);
        _gameplayChat?.Close();
        _commerceScreen?.Close();
        _commerceScreen?.Bind(null, null, null, null, null);
        _spellScreen?.Close();
        _spellScreen?.Bind(null, null, null);
        _gameplayHud!.SpellAssignMode = false;
        _fogOverlay?.Bind(null);
        _audio?.StopWorldAudio();
        _worldHost!.Visible = true;
        _hud!.Visible = true;
    }

    private void OnWorldEntered(WorldSession world, FrameConnection connection)
    {
        world.MapsWorld = _resources?.MapsWorld;
        world.SetMap(world.MapId, world.MapResource);
        MovementDiagnostics.ClearLog();
        MovementDiagnostics.LogSession(world, "WORLD_ENTER",
            $"spawn=({world.TileX},{world.TileY}) charIndex={world.CharIndex}");
        _gameplay = new GameplaySession(connection, world, _audio, _resources?.Grhs);
        _audio?.BindListener(world);
        _commerceScreen!.Bind(
            world,
            _resources,
            (slot, qty) => _gameplay.SendCommerceBuyAsync(slot, qty),
            (slot, qty) => _gameplay.SendCommerceSellAsync(slot, qty),
            () => _gameplay.SendCommerceEndAsync());
        _spellScreen!.Bind(world, _resources?.Spells, _gameplay);
        _gameplay.CommerceStarted += vendor => _pendingCommerceVendor = vendor;
        _gameplay.CommerceEnded += () => _pendingCommerceClose = true;
        if (_audio is not null && world.Map is not null)
        {
            MapAudioService.OnMapLoaded(world.Map, _audio);
        }
        _gameplay.Changed += () => CallDeferred(MethodName.OnGameplayChanged);
        _gameplay.Start();
        _worldView!.Visible = true;
        _hud!.Visible = false;
        _gameplayHud!.Bind(world, _resources);
        _gameplayConsole!.Bind(world);
        _fogOverlay?.Bind(world);
        RedrawWorld();
    }

    public override void _Process(double delta)
    {
        var win = GetWindow();
        if (win.Size != _lastLayoutWindowSize)
        {
            _lastLayoutWindowSize = win.Size;
            GameViewport.RestoreLayout(win);
        }

        if (_pendingCommerceClose)
        {
            _pendingCommerceClose = false;
            _commerceScreen?.Close();
        }
        if (_pendingCommerceVendor is { } vendor)
        {
            _pendingCommerceVendor = null;
            _commerceScreen?.Open(vendor);
        }

        _gameplay?.PumpUiQueue();

        UpdateItemTooltip();
        UpdateFps(delta);
        if (_pauseMenu?.IsOpen == true || _pauseActionRunning)
        {
            return;
        }
        if (_gameplay is null || _state.Current != ClientState.World)
        {
            return;
        }
        UpdateHud();
        _walkCooldown -= delta;
        _attackCooldown -= delta;
        _spellCooldown -= delta;
        if (_gameplay.IsWarping)
        {
            return;
        }
        if (_gameplayChat?.IsOpen == true)
        {
            return;
        }
        if (TryResucitate())
        {
            return;
        }
        if (TrySpellPanel() || TryCastSpellHotkey())
        {
            return;
        }
        if (TryMeditate() || TryHide() || TryCommerce())
        {
            return;
        }
        if (TryAttack())
        {
            return;
        }
        var heading = ReadHeadingInput();
        if (heading == 0)
        {
            return;
        }
        if (_gameplay.World.Motion.IsMoving)
        {
            return;
        }
        if (_walkCooldown > 0)
        {
            return;
        }
        if (!_gameplay.World.PredictWalk(heading))
        {
            return;
        }
        var walkSeq = System.Threading.Interlocked.Increment(ref _walkSendSeq);
        MovementDiagnostics.LogSession(_gameplay.World, "WALK_SEND", $"heading={heading} seq={walkSeq}");
        _walkCooldown = _gameplay.World.WalkIntervalMs / 1000.0;
        CallDeferred(MethodName.RedrawWorld);
        _ = _gameplay.SendWalkAsync(heading, walkSeq);
    }

    private void OnGameplayChanged()
    {
        if (_gameplay is not null)
        {
            if (_wasWarping && !_gameplay.IsWarping)
            {
                _walkCooldown = _gameplay.World.WalkIntervalMs / 1000.0;
            }
            _wasWarping = _gameplay.IsWarping;
        }
        _gameplayHud?.Refresh();
        _gameplayConsole?.QueueRedraw();
        UpdateSpellCursor();
        _commerceScreen?.QueueRedraw();
        CallDeferred(MethodName.RedrawWorld);
    }

    private void UpdateFps(double delta)
    {
        _fpsAccum += delta;
        _fpsFrames++;
        if (_fpsAccum < 0.5)
        {
            return;
        }
        _displayFps = (int)Math.Round(_fpsFrames / _fpsAccum);
        _fpsAccum = 0;
        _fpsFrames = 0;
        _fpsLabel!.Text = $"FPS {_displayFps}";
    }

    private void UpdateItemTooltip()
    {
        if (_floorItemTooltip is null || _gameplay is null || _worldHost is null)
        {
            return;
        }
        if (_state.Current != ClientState.World
            || _pauseMenu?.IsOpen == true
            || _commerceScreen?.IsOpen == true
            || _spellScreen?.IsOpen == true
            || _gameplayChat?.IsOpen == true
            || _gameplay.IsWarping
            || _gameplay.World.IsDead)
        {
            _floorItemTooltip.HideTooltip();
            return;
        }

        var globalMouse = GetViewport().GetMousePosition();
        if (TryShowInventoryItemTooltip(globalMouse))
        {
            return;
        }

        if (!GameViewport.TryMapGlobalMouseToLogical(_worldHost, globalMouse, out var worldLocal))
        {
            _floorItemTooltip.HideTooltip();
            return;
        }
        var screen = GameViewport.LogicalSize;
        if (!FloorItemPicker.TryPick(_gameplay.World, _resources, worldLocal, screen, out var hit))
        {
            _floorItemTooltip.HideTooltip();
            return;
        }
        var item = _resources?.Items?.Get(hit.ObjectIndex);
        var anchorGlobal = GameViewport.MapLogicalPointToGlobal(_worldHost, hit.AnchorScreen);
        _floorItemTooltip.ShowItem(anchorGlobal, item, hit.Amount, hit.ElementalTags);
    }

    private bool TryShowInventoryItemTooltip(Vector2 globalMouse)
    {
        if (_floorItemTooltip is null || _gameplayHud is null || _gameplay is null || !_gameplayHud.IsInventoryOpen)
        {
            return false;
        }

        var hudRect = _gameplayHud.GetGlobalRect();
        if (!hudRect.HasPoint(globalMouse))
        {
            return false;
        }

        var local = globalMouse - hudRect.Position;
        if (!_gameplayHud.TryGetHoveredInventoryItem(local, out var slot, out var anchorLocal))
        {
            return false;
        }

        var item = _gameplay.World.Inventory.GetSlot(slot);
        if (item.IsEmpty)
        {
            return false;
        }

        var itemDef = _resources?.Items?.Get(item.ObjIndex);
        _floorItemTooltip.ShowItem(hudRect.Position + anchorLocal, itemDef, item.Amount, item.ElementalTags, item.Equipped);
        return true;
    }

    private bool TryPickHoveredFloorItem()
    {
        if (_gameplay is null || _worldHost is null
            || _state.Current != ClientState.World
            || _pauseMenu?.IsOpen == true
            || _commerceScreen?.IsOpen == true
            || _spellScreen?.IsOpen == true
            || _gameplayChat?.IsOpen == true
            || _gameplay.IsWarping
            || _gameplay.World.IsDead)
        {
            return false;
        }
        var globalMouse = GetViewport().GetMousePosition();
        if (!GameViewport.TryMapGlobalMouseToLogical(_worldHost, globalMouse, out var worldLocal))
        {
            return false;
        }
        if (!FloorItemPicker.TryPick(_gameplay.World, _resources, worldLocal, GameViewport.LogicalSize, out var hit))
        {
            return false;
        }
        _ = _gameplay.SendPickUpAsync(hit.TileX, hit.TileY);
        return true;
    }

    private void UpdateHud()
    {
        if (_gameplayHud is not null)
        {
            _gameplayHud.SpellAssignMode = _spellScreen?.IsOpen == true;
        }
        _gameplayHud?.Refresh();
        UpdateSpellCursor();
    }

    private void UpdateSpellCursor()
    {
        var targeting = _gameplay?.World.UsingSkill == 1;
        if (targeting)
        {
            Input.SetDefaultCursorShape(Input.CursorShape.Cross);
            if (_worldHost is not null)
            {
                _worldHost.MouseDefaultCursorShape = Control.CursorShape.Cross;
            }
        }
        else
        {
            Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
            if (_worldHost is not null)
            {
                _worldHost.MouseDefaultCursorShape = Control.CursorShape.Arrow;
            }
        }
    }

    private static int ReadHeadingInput()
    {
        if (Input.IsActionPressed("ao_walk_up") || Input.IsActionPressed("ui_up") || Input.IsKeyPressed(Key.Up) || Input.IsKeyPressed(Key.W))
        {
            return 1;
        }
        if (Input.IsActionPressed("ao_walk_right") || Input.IsActionPressed("ui_right") || Input.IsKeyPressed(Key.Right) || Input.IsKeyPressed(Key.D))
        {
            return 2;
        }
        if (Input.IsActionPressed("ao_walk_down") || Input.IsActionPressed("ui_down") || Input.IsKeyPressed(Key.Down) || Input.IsKeyPressed(Key.S))
        {
            return 3;
        }
        if (Input.IsActionPressed("ao_walk_left") || Input.IsActionPressed("ui_left") || Input.IsKeyPressed(Key.Left) || Input.IsKeyPressed(Key.A))
        {
            return 4;
        }
        return 0;
    }

    private bool TryResucitate()
    {
        if (_gameplay is null || !_gameplay.World.IsDead)
        {
            return false;
        }
        if (!Input.IsActionJustPressed("ao_resucitate"))
        {
            return false;
        }
        _gameplay.World.DeathMessage = null;
        _ = _gameplay.SendResucitateAsync();
        return true;
    }

    private bool TryLeftClick(Vector2 worldLocalPos)
    {
        if (_gameplay is null || _gameplay.World.IsDead || _worldHost is null)
        {
            return false;
        }
        var screen = GameViewport.LogicalSize;
        var camera = WorldCamera.Create(_gameplay.World, screen);
        if (_gameplay.World.UsingSkill == 1)
        {
            if (_spellCooldown > 0)
            {
                return true;
            }
            if (!SpellTargetPicker.TryResolveTile(_gameplay.World, _resources, worldLocalPos, screen, out var tileX, out var tileY))
            {
                return true;
            }
            _spellCooldown = _gameplay.World.MagicIntervalMs / 1000.0;
            _ = _gameplay.SendWorkLeftClickAsync(tileX, tileY, 1);
            return true;
        }
        if (!camera.TryScreenToTile(worldLocalPos, out var clickX, out var clickY))
        {
            return false;
        }
        _ = _gameplay.SendLeftClickAsync(clickX, clickY);
        return true;
    }

    private bool TrySpellPanel()
    {
        if (_gameplay is null || _gameplay.World.IsDead)
        {
            return false;
        }
        if (!Input.IsActionJustPressed("ao_spells"))
        {
            return false;
        }
        _spellScreen?.Toggle();
        _gameplayHud!.SpellAssignMode = _spellScreen?.IsOpen == true;
        return true;
    }

    private bool TryCastSpellHotkey()
    {
        if (_gameplay is null || _gameplay.World.IsDead)
        {
            return false;
        }
        if (!Input.IsActionJustPressed("ao_cast_spell"))
        {
            return false;
        }
        if (_spellScreen?.IsOpen == true)
        {
            _spellScreen.CastSelected();
        }
        else
        {
            CastSpellFromHotkey(1);
        }
        return true;
    }

    private bool TryMeditate()
    {
        if (_gameplay is null || _gameplay.World.IsDead)
        {
            return false;
        }
        if (!Input.IsActionJustPressed("ao_meditate"))
        {
            return false;
        }
        _ = _gameplay.SendMeditateAsync();
        return true;
    }

    private bool TryHide()
    {
        if (_gameplay is null || _gameplay.World.IsDead || _gameplay.World.Motion.IsMoving)
        {
            return false;
        }
        if (!Input.IsActionJustPressed("ao_hide"))
        {
            return false;
        }
        _ = _gameplay.SendHideAsync();
        return true;
    }

    private bool TryCommerce()
    {
        if (_gameplay is null || _gameplay.World.IsDead)
        {
            return false;
        }
        if (!Input.IsActionJustPressed("ao_commerce"))
        {
            return false;
        }
        _ = _gameplay.SendCommerceStartAsync();
        return true;
    }

    private bool TryAttack()
    {
        if (_gameplay is null)
        {
            return false;
        }
        if (!WantsAttack())
        {
            return false;
        }
        if (_gameplay.World.IsDead || _gameplay.World.Motion.IsMoving)
        {
            return false;
        }
        if (_attackCooldown > 0)
        {
            return true;
        }
        _attackCooldown = _gameplay.World.AttackIntervalMs / 1000.0;
        CallDeferred(MethodName.RedrawWorld);
        _ = _gameplay.SendAttackAsync();
        return true;
    }

    private static bool WantsAttack()
    {
        if (Input.IsActionPressed("ao_attack") || Input.IsPhysicalKeyPressed(Key.Ctrl))
        {
            return true;
        }
        return Input.IsPhysicalKeyPressed(Key.Ctrl) && Input.IsMouseButtonPressed(MouseButton.Left);
    }

    private void RedrawWorld()
    {
        if (_gameplay is null)
        {
            return;
        }
        foreach (var block in _gameplay.Blocks)
        {
            _gameplay.World.Blocks[block.Key] = block.Value;
        }
        _gameplay.World.MapsWorld ??= _resources?.MapsWorld;
        _worldView!.Bind(_gameplay.World, _resources);
        UpdateHud();
    }

    public override void _ExitTree()
    {
        _gameplay?.Dispose();
    }
}
