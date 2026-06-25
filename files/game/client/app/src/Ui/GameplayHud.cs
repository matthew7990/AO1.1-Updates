using System;
using System.Collections.Generic;
using System.Globalization;
using Argentum.Client.Core;
using Argentum.Client.Resources;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>HUD MMORPG: orbes HP/Maná a los lados, hotbar+EXP abajo al centro, mapa arriba-derecha.</summary>
public partial class GameplayHud : Control
{
    private static readonly Color PanelFill = new(0.05f, 0.06f, 0.1f, 0.92f);
    private static readonly Color PanelBorder = new(0.68f, 0.58f, 0.36f, 0.6f);
    private static readonly Color PanelHighlight = new(0.88f, 0.76f, 0.44f, 0.4f);
    private static readonly Color TextPrimary = new("ebe4d6");
    private static readonly Color TextMuted = new(0.58f, 0.56f, 0.52f);
    private static readonly Color HpCore = new("e04a4a");
    private static readonly Color HpGlow = new("ff8a7a");
    private static readonly Color HpEmpty = new(0.14f, 0.06f, 0.06f, 0.92f);
    private static readonly Color ManaCore = new("3a7fd4");
    private static readonly Color ManaGlow = new("7ab8ff");
    private static readonly Color ManaEmpty = new(0.06f, 0.1f, 0.18f, 0.92f);
    private static readonly Color ExpColor = new("d9a441");
    private static readonly Color ExpBack = new(0.14f, 0.1f, 0.05f, 0.88f);
    private static readonly Color SlotFill = new(0.1f, 0.11f, 0.16f, 0.95f);
    private static readonly Color SlotBorder = new(0.45f, 0.4f, 0.32f, 0.7f);
    private static readonly Color SlotEquipped = new(0.75f, 0.62f, 0.28f, 0.85f);
    private static readonly Color SlotLocked = new(0.06f, 0.06f, 0.08f, 0.85f);
    private static readonly EquipmentSlot[] EquipmentSlots =
    [
        new EquipmentSlot("Arma", 2),
        new EquipmentSlot("Armadura", 3),
        new EquipmentSlot("Escudo", 16),
        new EquipmentSlot("Casco", 17),
        new EquipmentSlot("Accesorio", 21),
    ];
    private const float EquipmentPanelH = 104f;
    private const float InventoryGridTop = 180f;

    private WorldSession? _world;
    private GameResources? _resources;
    private SpellCatalog? _spells;
    private bool _inventoryOpen;
    private bool _characterPanelOpen;
    private Vector2 _screenSize;
    private GameplayHudLayout _layout;
    private bool _spellAssignMode;
    private Vector2? _inventoryPanelPos;
    private bool _draggingInventoryPanel;
    private Vector2 _inventoryDragOffset;
    private Vector2? _characterPanelPos;
    private bool _draggingCharacterPanel;
    private Vector2 _characterDragOffset;
    private CharacterPanelTab _characterTab = CharacterPanelTab.Resumen;

    public bool SpellAssignMode
    {
        get => _spellAssignMode;
        set
        {
            if (_spellAssignMode == value)
            {
                return;
            }
            _spellAssignMode = value;
            QueueRedraw();
        }
    }

    public bool IsInventoryOpen => _inventoryOpen;
    public bool IsCharacterPanelOpen => _characterPanelOpen;

    public void ToggleInventory()
    {
        _inventoryOpen = !_inventoryOpen;
        if (_inventoryOpen)
        {
            EnsureInventoryPanelPosition(BuildLayout(AoUiFonts.Ui));
        }
        QueueRedraw();
    }

    public void ToggleCharacterPanel()
    {
        _characterPanelOpen = !_characterPanelOpen;
        if (_characterPanelOpen)
        {
            EnsureCharacterPanelPosition(BuildLayout(AoUiFonts.Ui));
        }
        QueueRedraw();
    }

    public bool TryHitInventorySlot(Vector2 localPos, out int slot) =>
        TryGetInventoryGridSlotAtPosition(localPos, out slot, out _);

    public bool TryGetHoveredInventoryItem(Vector2 localPos, out int slot, out Vector2 anchorLocal)
    {
        slot = 0;
        anchorLocal = Vector2.Zero;
        if (_world is null || !_inventoryOpen)
        {
            return false;
        }

        if (TryGetEquipmentItemAtPosition(localPos, out slot, out var rect)
            || TryGetInventoryGridSlotAtPosition(localPos, out slot, out rect))
        {
            var item = _world.Inventory.GetSlot(slot);
            if (!item.IsEmpty)
            {
                anchorLocal = rect.Position + new Vector2(rect.Size.X + 10f, rect.Size.Y * 0.5f);
                return true;
            }
        }

        return false;
    }

    public bool HandleInventoryPointerInput(InputEvent @event, Vector2 localPos)
    {
        if (_world is null)
        {
            return false;
        }

        var layout = BuildLayout(AoUiFonts.Ui);
        if (HandleCharacterPanelPointerInput(@event, localPos, layout))
        {
            return true;
        }
        if (HandleInventoryPanelPointerInput(@event, localPos, layout))
        {
            return true;
        }
        return false;
    }

    private bool HandleInventoryPanelPointerInput(InputEvent @event, Vector2 localPos, GameplayHudLayout layout)
    {
        if (!_inventoryOpen)
        {
            return false;
        }

        var panel = GetInventoryPanelRect(layout);
        var titleBar = new Rect2(panel.Position, new Vector2(panel.Size.X, 31f));

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            if (button.Pressed && titleBar.HasPoint(localPos))
            {
                _draggingInventoryPanel = true;
                _inventoryDragOffset = localPos - panel.Position;
                return true;
            }
            if (!button.Pressed && _draggingInventoryPanel)
            {
                _draggingInventoryPanel = false;
                return true;
            }
            return panel.HasPoint(localPos);
        }

        if (@event is InputEventMouseMotion && _draggingInventoryPanel)
        {
            var size = GetInventoryPanelSize();
            _inventoryPanelPos = ClampInventoryPanelPosition(localPos - _inventoryDragOffset, size);
            QueueRedraw();
            return true;
        }

        return false;
    }

    private bool HandleCharacterPanelPointerInput(InputEvent @event, Vector2 localPos, GameplayHudLayout layout)
    {
        if (!_characterPanelOpen)
        {
            return false;
        }

        var panel = GetCharacterPanelRect(layout);
        var titleBar = new Rect2(panel.Position, new Vector2(panel.Size.X, 33f));

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            if (button.Pressed)
            {
                if (TryGetCharacterTabAtPosition(localPos, panel, out var tab))
                {
                    _characterTab = tab;
                    QueueRedraw();
                    return true;
                }
                if (titleBar.HasPoint(localPos))
                {
                    _draggingCharacterPanel = true;
                    _characterDragOffset = localPos - panel.Position;
                    return true;
                }
            }
            if (!button.Pressed && _draggingCharacterPanel)
            {
                _draggingCharacterPanel = false;
                return true;
            }
            return panel.HasPoint(localPos);
        }

        if (@event is InputEventMouseMotion && _draggingCharacterPanel)
        {
            var size = GetCharacterPanelSize();
            _characterPanelPos = ClampCharacterPanelPosition(localPos - _characterDragOffset, size);
            QueueRedraw();
            return true;
        }

        return false;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        SyncToViewport();
        GetViewport().SizeChanged += OnViewportResized;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            SyncToViewport();
        }
    }

    private void OnViewportResized() => SyncToViewport();

    private void SyncToViewport()
    {
        _screenSize = GameViewport.GetRenderSize(GetViewport());
        if (_screenSize.X < 1f || _screenSize.Y < 1f)
        {
            _screenSize = GetViewportRect().Size;
        }
        Position = Vector2.Zero;
        Size = _screenSize;
        if (_inventoryPanelPos.HasValue)
        {
            _inventoryPanelPos = ClampInventoryPanelPosition(_inventoryPanelPos.Value, GetInventoryPanelSize());
        }
        if (_characterPanelPos.HasValue)
        {
            _characterPanelPos = ClampCharacterPanelPosition(_characterPanelPos.Value, GetCharacterPanelSize());
        }
        QueueRedraw();
    }

    public void Bind(WorldSession? world, GameResources? resources)
    {
        _world = world;
        _resources = resources;
        _spells = resources?.Spells;
        Visible = world is not null;
        SyncToViewport();
    }

    public bool TryHitSpellHotbar(Vector2 localPos, out int hotkey)
    {
        hotkey = 0;
        if (_world is null)
        {
            return false;
        }
        var layout = BuildLayout(AoUiFonts.Ui);
        for (var i = 1; i <= SpellHotbar.SlotCount; i++)
        {
            var slotX = layout.HotbarX + (i - 1) * (GameplayHudLayout.SlotSize + GameplayHudLayout.SlotGap);
            var rect = new Rect2(slotX, layout.HotbarY, GameplayHudLayout.SlotSize, GameplayHudLayout.SlotSize);
            if (rect.HasPoint(localPos))
            {
                hotkey = i;
                return true;
            }
        }
        return false;
    }

    private Rect2 GetSpellHotbarRect(int hotkey)
    {
        var slotX = _layout.HotbarX + (hotkey - 1) * (GameplayHudLayout.SlotSize + GameplayHudLayout.SlotGap);
        return new Rect2(slotX, _layout.HotbarY, GameplayHudLayout.SlotSize, GameplayHudLayout.SlotSize);
    }

    public void Refresh()
    {
        SyncToViewport();
    }

    public override void _Draw()
    {
        if (_world is null)
        {
            return;
        }

        var font = AoUiFonts.Ui;
        _layout = BuildLayout(font);

        DrawMapChip(font, _layout);
        DrawBottomExpBar(font, _layout);
        DrawHotbar(_layout);
        DrawPlayerCaption(font, _layout);

        DrawResourceOrb(font, _layout.LeftOrbCenter, _world.MinHp, _world.MaxHp, "Vida", HpCore, HpGlow, HpEmpty);
        DrawResourceOrb(font, _layout.RightOrbCenter, _world.MinMana, _world.MaxMana, "Maná", ManaCore, ManaGlow, ManaEmpty);

        if (_world.MaxSta > 0)
        {
            var sta = $"{_world.MinSta}/{_world.MaxSta}";
            HudTextDraw.AtTopCenter(this, font, _layout.CenterX, _layout.StaminaTop, sta, 9, TextMuted);
        }

        if (_inventoryOpen)
        {
            DrawInventoryPanel(font, _layout);
        }
        if (_characterPanelOpen)
        {
            DrawCharacterPanel(font, _layout);
        }
    }

    private GameplayHudLayout BuildLayout(Font font)
    {
        var mapName = _world!.Map?.Name;
        if (string.IsNullOrWhiteSpace(mapName))
        {
            mapName = $"Mapa {_world.MapId}";
        }
        var coords = $"({_world.TileX}, {_world.TileY})";
        var chipW = MathF.Max(
            font.GetStringSize(mapName, fontSize: 12).X,
            font.GetStringSize(coords, fontSize: 10).X) + 24f;
        return GameplayHudLayout.FromViewport(_screenSize, chipW);
    }

    private void DrawPlayerCaption(Font font, GameplayHudLayout layout)
    {
        var name = string.IsNullOrWhiteSpace(_world!.CharacterName) ? "Aventurero" : _world.CharacterName;
        var caption = _world.Gold > 0
            ? $"{name}  ·  Nv {_world.Level}  ·  {FormatNumber(_world.Gold)} oro"
            : $"{name}  ·  Nv {_world.Level}";
        HudTextDraw.AtTopCenter(this, font, layout.CenterX, layout.CaptionTop, caption, 11, TextPrimary);
    }

    private void DrawResourceOrb(Font font, Vector2 center, int current, int max, string label, Color core, Color glow, Color empty)
    {
        var ratio = Math.Clamp(current / (float)Math.Max(1, max), 0f, 1f);
        var radius = GameplayHudLayout.OrbMinR + (GameplayHudLayout.OrbMaxR - GameplayHudLayout.OrbMinR) * ratio;

        DrawCircle(center + new Vector2(2, 3), GameplayHudLayout.OrbMaxR + 4f, new Color(0, 0, 0, 0.35f));
        DrawCircle(center, GameplayHudLayout.OrbMaxR + 3f, new Color(core, 0.18f));
        DrawCircle(center, GameplayHudLayout.OrbMaxR + 1.5f, new Color(PanelBorder, 0.55f));
        DrawCircle(center, GameplayHudLayout.OrbMaxR, empty);

        if (radius > 1f)
        {
            DrawCircle(center, radius + 2f, new Color(glow, 0.35f));
            DrawCircle(center, radius, core);
            DrawCircle(center - new Vector2(radius * 0.22f, radius * 0.28f), radius * 0.28f,
                new Color(1f, 1f, 1f, 0.14f));
        }

        var value = $"{current}";
        HudTextDraw.Centered(this, font, center, value, 13, TextPrimary);

        var labelTop = center.Y + GameplayHudLayout.OrbMaxR + 8f;
        HudTextDraw.AtTopCenter(this, font, center.X, labelTop, label, 10, TextMuted);
    }

    private void DrawBottomExpBar(Font font, GameplayHudLayout layout)
    {
        var x = layout.CenterX - GameplayHudLayout.ExpBarW * 0.5f;
        var y = layout.ExpY;
        var exp = Math.Max(0, _world!.Exp);
        var next = Math.Max(0, _world.ExpNext);
        var ratio = next > 0 ? Math.Clamp(exp / (float)next, 0f, 1f) : 1f;

        DrawRect(new Rect2(x - 2, y - 2, GameplayHudLayout.ExpBarW + 4, GameplayHudLayout.ExpBarH + 4),
            new Color(0, 0, 0, 0.4f));
        DrawRect(new Rect2(x, y, GameplayHudLayout.ExpBarW, GameplayHudLayout.ExpBarH), ExpBack);
        if (ratio > 0f)
        {
            var fillW = MathF.Max(2f, GameplayHudLayout.ExpBarW * ratio);
            DrawRect(new Rect2(x, y, fillW, GameplayHudLayout.ExpBarH), ExpColor);
            DrawRect(new Rect2(x, y + 1, fillW, 2), new Color(1, 1, 1, 0.12f));
        }

        var pct = next > 0 ? $"{ratio * 100f:F0}%" : "MAX";
        HudTextDraw.AtTopCenter(this, font, layout.CenterX, layout.ExpLabelTop, pct, 9, ExpColor);
    }

    private void DrawHotbar(GameplayHudLayout layout)
    {
        for (var hotkey = 1; hotkey <= SpellHotbar.SlotCount; hotkey++)
        {
            DrawSpellHotbarSlot(GetSpellHotbarRect(hotkey), hotkey);
        }
        if (_spellAssignMode)
        {
            var hint = "Click en 1-0 para asignar hechizo";
            HudTextDraw.AtTopCenter(this, AoUiFonts.Ui, layout.CenterX, layout.HotbarY - 16f, hint, 9, new Color("a8c8ff"));
        }
    }

    private void DrawSpellHotbarSlot(Rect2 rect, int hotkey)
    {
        var spellBookSlot = _world!.SpellHotbar.GetSpellBookSlot(hotkey);
        var assignHighlight = _spellAssignMode;
        DrawRect(rect, assignHighlight ? new Color(0.12f, 0.16f, 0.24f, 0.98f) : SlotFill);
        DrawBorder(rect, assignHighlight ? new Color("6a9fd8") : SlotBorder);

        HudTextDraw.AtTopLeft(this, AoUiFonts.Ui, rect.Position + new Vector2(3, 2),
            SpellHotbar.HotkeyLabel(hotkey), 9, TextMuted);

        if (spellBookSlot <= 0)
        {
            return;
        }

        var spellId = _world.Spells.GetSlot(spellBookSlot);
        if (spellId <= 0)
        {
            return;
        }

        var iconGrh = _spells?.Get(spellId)?.IconoIndex ?? 0;
        if (iconGrh > 0)
        {
            DrawGrhIcon(iconGrh, rect.Grow(-5));
        }
        else
        {
            DrawRect(rect.Grow(-8), new Color(ManaCore, 0.35f));
        }
    }

    private void DrawGrhIcon(int grh, Rect2 dest)
    {
        if (grh <= 0 || _resources is null)
        {
            return;
        }
        var def = _resources.Grhs.ResolveDrawable(grh, (int)Time.GetTicksMsec());
        if (def is null || def.FileNum <= 0)
        {
            return;
        }
        var tex = _resources.Textures.Get(def.FileNum);
        if (tex is null)
        {
            return;
        }
        var fit = MathF.Min(dest.Size.X / def.PixelWidth, dest.Size.Y / def.PixelHeight);
        var scale = MathF.Min(1f, fit);
        var w = def.PixelWidth * scale;
        var h = def.PixelHeight * scale;
        var centered = new Rect2(
            dest.Position.X + (dest.Size.X - w) * 0.5f,
            dest.Position.Y + (dest.Size.Y - h) * 0.5f,
            w, h);
        var src = new Rect2(def.SX, def.SY, def.PixelWidth, def.PixelHeight);
        DrawTextureRectRegion(tex, centered, src);
    }

    private void DrawInventoryPanel(Font font, GameplayHudLayout layout)
    {
        var panel = GetInventoryPanelRect(layout);
        var panelX = panel.Position.X;
        var panelY = panel.Position.Y;
        var panelW = panel.Size.X;
        var panelH = panel.Size.Y;

        DrawPanel(new Vector2(panelX, panelY), new Vector2(panelW, panelH));
        DrawAccent(new Vector2(panelX + 14, panelY + 10), panelW - 28);
        HudTextDraw.AtTopLeft(this, font, new Vector2(panelX + 14, panelY + 14), "Inventario / Equipo", 12, TextPrimary);
        HudTextDraw.AtTopRight(this, font, panelX + panelW - 14, panelY + 14, "[I] cerrar", 10, TextMuted);

        DrawEquipmentPanel(font, new Vector2(panelX + 14, panelY + 38), panelW - 28);
        HudTextDraw.AtTopLeft(this, font, new Vector2(panelX + 14, panelY + InventoryGridTop - 22f), "Inventario", 11, TextMuted);
        HudTextDraw.AtTopCenter(this, font, panelX + panelW * 0.5f, panelY + panelH - 16f, "Doble click en item para usar", 9, TextMuted);

        var grid = GetInventoryGridPosition(panel.Position);
        var gridX = grid.X;
        var gridY = grid.Y;
        for (var slot = 1; slot <= _world!.Inventory.UnlockedSlots; slot++)
        {
            var col = (slot - 1) % GameplayHudLayout.InvCols;
            var row = (slot - 1) / GameplayHudLayout.InvCols;
            var pos = new Vector2(
                gridX + col * (GameplayHudLayout.SlotSize + GameplayHudLayout.SlotGap),
                gridY + row * (GameplayHudLayout.SlotSize + GameplayHudLayout.SlotGap));
            DrawInventorySlot(pos, slot, showKey: 0);
        }
    }

    private void DrawEquipmentPanel(Font font, Vector2 pos, float width)
    {
        var panel = new Rect2(pos, new Vector2(width, EquipmentPanelH));
        DrawRect(panel, new Color(0.07f, 0.08f, 0.12f, 0.92f));
        DrawBorder(panel, new Color(PanelBorder, 0.72f));
        DrawAccent(pos + new Vector2(10f, 8f), width - 20f);
        HudTextDraw.AtTopLeft(this, font, pos + new Vector2(10f, 15f), "Equipo", 12, TextPrimary);

        for (var i = 0; i < EquipmentSlots.Length; i++)
        {
            var rect = GetEquipmentSlotRect(pos, width, i);
            DrawRect(rect, new Color(0.1f, 0.11f, 0.16f, 0.96f));
            DrawBorder(rect, SlotBorder);
            HudTextDraw.AtTopCenter(this, font, rect.Position.X + rect.Size.X * 0.5f, rect.Position.Y + 4f, EquipmentSlots[i].Label, 8, TextMuted);

            var itemSlot = FindEquippedSlot(EquipmentSlots[i].ObjType);
            if (itemSlot <= 0)
            {
                HudTextDraw.Centered(this, font, rect.GetCenter() + new Vector2(0, 7), "-", 12, TextMuted);
                continue;
            }

            var item = _world!.Inventory.GetSlot(itemSlot);
            DrawItemIcon(item.ObjIndex, new Rect2(rect.Position + new Vector2((rect.Size.X - 32f) * 0.5f, 21f), new Vector2(32f, 32f)), item.ElementalTags, equipped: true);
        }
    }

    private int FindEquippedSlot(int objType)
    {
        for (var slot = 1; slot <= PlayerInventory.MaxSlots; slot++)
        {
            var item = _world!.Inventory.GetSlot(slot);
            if (item.IsEmpty || !item.Equipped)
            {
                continue;
            }
            var itemDef = _resources?.Items?.Get(item.ObjIndex);
            var itemObjType = itemDef?.ObjType ?? _resources?.Objects.Get(item.ObjIndex)?.ObjType ?? 0;
            if (itemObjType == objType)
            {
                return slot;
            }
        }
        return 0;
    }

    private Rect2 GetInventoryPanelRect(GameplayHudLayout layout)
    {
        var size = GetInventoryPanelSize();
        EnsureInventoryPanelPosition(layout);
        return new Rect2(_inventoryPanelPos ?? GetDefaultInventoryPanelPosition(layout, size), size);
    }

    private Vector2 GetInventoryPanelSize()
    {
        var unlocked = _world?.Inventory.UnlockedSlots ?? PlayerInventory.DefaultUnlocked;
        var rows = (int)Math.Ceiling(unlocked / (float)GameplayHudLayout.InvCols);
        var panelW = GameplayHudLayout.InvCols * (GameplayHudLayout.SlotSize + GameplayHudLayout.SlotGap)
                     - GameplayHudLayout.SlotGap + 28f;
        var panelH = rows * (GameplayHudLayout.SlotSize + GameplayHudLayout.SlotGap)
                     - GameplayHudLayout.SlotGap + InventoryGridTop + 24f;
        return new Vector2(panelW, panelH);
    }

    private void EnsureInventoryPanelPosition(GameplayHudLayout layout)
    {
        if (_inventoryPanelPos.HasValue)
        {
            _inventoryPanelPos = ClampInventoryPanelPosition(_inventoryPanelPos.Value, GetInventoryPanelSize());
            return;
        }
        var size = GetInventoryPanelSize();
        _inventoryPanelPos = GetDefaultInventoryPanelPosition(layout, size);
    }

    private Vector2 GetDefaultInventoryPanelPosition(GameplayHudLayout layout, Vector2 size)
    {
        var x = layout.Screen.X - GameplayHudLayout.Margin - size.X;
        var y = MathF.Max(GameplayHudLayout.Margin + 54f, layout.ExpY - 12f - size.Y);
        return ClampInventoryPanelPosition(new Vector2(x, y), size);
    }

    private Vector2 ClampInventoryPanelPosition(Vector2 pos, Vector2 size)
    {
        var maxX = MathF.Max(GameplayHudLayout.Margin, _screenSize.X - GameplayHudLayout.Margin - size.X);
        var maxY = MathF.Max(GameplayHudLayout.Margin, _screenSize.Y - GameplayHudLayout.Margin - size.Y);
        return new Vector2(
            Math.Clamp(pos.X, GameplayHudLayout.Margin, maxX),
            Math.Clamp(pos.Y, GameplayHudLayout.Margin, maxY));
    }

    private static Vector2 GetInventoryGridPosition(Vector2 panelPos) =>
        panelPos + new Vector2(14f, InventoryGridTop);

    private void DrawCharacterPanel(Font font, GameplayHudLayout layout)
    {
        var panel = GetCharacterPanelRect(layout);
        DrawPanel(panel.Position, panel.Size);
        DrawAccent(panel.Position + new Vector2(14f, 10f), panel.Size.X - 28f);
        HudTextDraw.AtTopLeft(this, font, panel.Position + new Vector2(14f, 14f), "Personaje", 13, TextPrimary);
        HudTextDraw.AtTopRight(this, font, panel.Position.X + panel.Size.X - 14f, panel.Position.Y + 14f, "[C] cerrar", 10, TextMuted);

        DrawCharacterTabBar(font, panel);

        var content = new Rect2(panel.Position + new Vector2(14f, 74f), panel.Size - new Vector2(28f, 88f));
        switch (_characterTab)
        {
            case CharacterPanelTab.Resumen:
                DrawCharacterSummaryTab(font, content);
                break;
            case CharacterPanelTab.Recursos:
                DrawCharacterResourcesTab(font, content);
                break;
            case CharacterPanelTab.Combate:
                DrawCharacterCombatTab(font, content);
                break;
            case CharacterPanelTab.Equipo:
                DrawCharacterEquipmentTab(font, content);
                break;
            case CharacterPanelTab.Mundo:
                DrawCharacterWorldTab(font, content);
                break;
        }
    }

    private void DrawCharacterTabBar(Font font, Rect2 panel)
    {
        for (var i = 0; i < CharacterPanelTabs.Length; i++)
        {
            var tab = CharacterPanelTabs[i];
            var rect = GetCharacterTabRect(panel, i);
            var active = tab == _characterTab;
            DrawRect(rect, active ? new Color(0.11f, 0.14f, 0.2f, 0.96f) : new Color(0.08f, 0.1f, 0.15f, 0.88f));
            DrawBorder(rect, active ? new Color(0.9f, 0.76f, 0.44f, 0.92f) : new Color(0.45f, 0.4f, 0.32f, 0.7f));
            HudTextDraw.Centered(this, font, rect.GetCenter(), CharacterPanelLabel(tab), 10, active ? TextPrimary : TextMuted);
        }
    }

    private void DrawCharacterSummaryTab(Font font, Rect2 rect)
    {
        DrawSectionCard(font, new Rect2(rect.Position, new Vector2(rect.Size.X * 0.5f - 6f, 146f)), "Perfil");
        DrawStatRows(font, rect.Position + new Vector2(14f, 34f), 18f,
            Row("Nombre", SafeName()),
            Row("Nivel", _world!.Level.ToString()),
            Row("Clase", ClassLabel(_world.ClassId)),
            Row("Experiencia", $"{FormatNumber(_world.Exp)} / {FormatNumber(_world.ExpNext)}"),
            Row("Oro", FormatNumber(_world.Gold)));

        var right = new Rect2(rect.Position + new Vector2(rect.Size.X * 0.5f + 6f, 0f), new Vector2(rect.Size.X * 0.5f - 6f, 146f));
        DrawSectionCard(font, right, "Estado");
        DrawStatRows(font, right.Position + new Vector2(14f, 34f), 18f,
            Row("Vida", $"{_world!.MinHp} / {_world.MaxHp}"),
            Row("Maná", $"{_world.MinMana} / {_world.MaxMana}"),
            Row("Energía", $"{_world.MinSta} / {_world.MaxSta}"),
            Row("Escudo", _world.Shield.ToString()),
            Row("Condición", CharacterStateLabel()));

        var bottom = new Rect2(rect.Position + new Vector2(0f, 160f), rect.Size - new Vector2(0f, 160f));
        DrawSectionCard(font, bottom, "Lectura rápida");
        DrawParagraph(font, bottom.Position + new Vector2(14f, 34f), bottom.Size.X - 28f,
            $"{SafeName()} está en nivel {_world!.Level}, clase {ClassLabel(_world.ClassId)}, con {_world.MinHp}/{_world.MaxHp} de vida, {_world.MinMana}/{_world.MaxMana} de maná y {_world.MinSta}/{_world.MaxSta} de energía.");
        DrawParagraph(font, bottom.Position + new Vector2(14f, 62f), bottom.Size.X - 28f,
            $"Progreso actual: {FormatNumber(_world.Exp)} de {FormatNumber(_world.ExpNext)} exp hacia el próximo nivel. Oro disponible: {FormatNumber(_world.Gold)}.");
    }

    private void DrawCharacterResourcesTab(Font font, Rect2 rect)
    {
        var left = new Rect2(rect.Position, new Vector2(rect.Size.X * 0.5f - 6f, rect.Size.Y));
        var right = new Rect2(rect.Position + new Vector2(rect.Size.X * 0.5f + 6f, 0f), new Vector2(rect.Size.X * 0.5f - 6f, rect.Size.Y));
        DrawSectionCard(font, left, "Recursos principales");
        DrawBigStat(font, left.Position + new Vector2(14f, 34f), "Vida", _world!.MinHp, _world.MaxHp, HpCore);
        DrawBigStat(font, left.Position + new Vector2(14f, 112f), "Maná", _world.MinMana, _world.MaxMana, ManaCore);
        DrawBigStat(font, left.Position + new Vector2(14f, 190f), "Energía", _world.MinSta, _world.MaxSta, new Color("d9a441"));

        DrawSectionCard(font, right, "Estado actual");
        DrawStatRows(font, right.Position + new Vector2(14f, 34f), 18f,
            Row("Escudo absorbido", _world!.Shield.ToString()),
            Row("Meditando", BoolLabel(_world.Meditating)),
            Row("Oculto", BoolLabel(_world.Hidden)),
            Row("Muerto", BoolLabel(_world.IsDead)),
            Row("Mensaje", string.IsNullOrWhiteSpace(_world.GameMessage) ? "—" : _world.GameMessage!));
    }

    private void DrawCharacterCombatTab(Font font, Rect2 rect)
    {
        var attackPower = ComputeAttackPower();
        var attackPerSec = 1000f / Math.Max(1, _world!.AttackIntervalMs);
        var castPerSec = 1000f / Math.Max(1, _world.MagicIntervalMs);
        var stepsPerSec = 1000f / Math.Max(1, _world.WalkIntervalMs);
        DrawSectionCard(font, rect, "Ritmos y combate");
        DrawStatRows(font, rect.Position + new Vector2(14f, 34f), 18f,
            Row("Poder de ataque base", attackPower.ToString()),
            Row("Ataque", $"{_world.AttackIntervalMs} ms · {attackPerSec:F2}/s"),
            Row("Lanzamiento", $"{_world.MagicIntervalMs} ms · {castPerSec:F2}/s"),
            Row("Movimiento", $"{_world.WalkIntervalMs} ms · {stepsPerSec:F2} pasos/s"),
            Row("Escudo actual", _world.Shield.ToString()),
            Row("Arma gráfica", _world.GfxWeapon.ToString()),
            Row("Supervivencia actual", $"Vida {_world.MinHp}/{_world.MaxHp} · Escudo {_world.Shield}"));

        DrawParagraph(font, rect.Position + new Vector2(14f, 188f), rect.Size.X - 28f,
            "Este panel usa únicamente datos reales del estado actual: ritmos de ataque, magia y movimiento sincronizados por el servidor, más el poder base de ataque derivado de la fórmula simplificada que hoy usa server-go.");
    }

    private void DrawCharacterEquipmentTab(Font font, Rect2 rect)
    {
        DrawSectionCard(font, rect, "Equipo activo");
        var y = rect.Position.Y + 34f;
        foreach (var row in EnumerateEquippedItemRows())
        {
            DrawEquipmentRow(font, new Rect2(rect.Position.X + 10f, y, rect.Size.X - 20f, 54f), row);
            y += 60f;
        }
    }

    private void DrawCharacterWorldTab(Font font, Rect2 rect)
    {
        DrawSectionCard(font, rect, "Mundo y sesión");
        DrawStatRows(font, rect.Position + new Vector2(14f, 34f), 18f,
            Row("Mapa", MapLabel()),
            Row("Posición", $"{_world!.TileX}, {_world.TileY}"),
            Row("Índice de usuario", _world.UserIndex.ToString()),
            Row("Índice de personaje", _world.CharIndex.ToString()),
            Row("Privilegios", _world.Privilege.ToString()),
            Row("Inventario desbloq.", _world.Inventory.UnlockedSlots.ToString()),
            Row("Hechizo en objetivo", _world.UsingSkill.ToString()),
            Row("Área activa", BoolLabel(_world.AreaSpellCast)));
    }

    private Rect2 GetCharacterPanelRect(GameplayHudLayout layout)
    {
        var size = GetCharacterPanelSize();
        EnsureCharacterPanelPosition(layout);
        return new Rect2(_characterPanelPos ?? GetDefaultCharacterPanelPosition(layout, size), size);
    }

    private Vector2 GetCharacterPanelSize() => new(560f, 396f);

    private void EnsureCharacterPanelPosition(GameplayHudLayout layout)
    {
        if (_characterPanelPos.HasValue)
        {
            _characterPanelPos = ClampCharacterPanelPosition(_characterPanelPos.Value, GetCharacterPanelSize());
            return;
        }
        var size = GetCharacterPanelSize();
        _characterPanelPos = GetDefaultCharacterPanelPosition(layout, size);
    }

    private Vector2 GetDefaultCharacterPanelPosition(GameplayHudLayout layout, Vector2 size)
    {
        var x = GameplayHudLayout.Margin + 24f;
        var y = MathF.Max(GameplayHudLayout.Margin + 56f, layout.ExpY - 18f - size.Y);
        return ClampCharacterPanelPosition(new Vector2(x, y), size);
    }

    private Vector2 ClampCharacterPanelPosition(Vector2 pos, Vector2 size)
    {
        var maxX = MathF.Max(GameplayHudLayout.Margin, _screenSize.X - GameplayHudLayout.Margin - size.X);
        var maxY = MathF.Max(GameplayHudLayout.Margin, _screenSize.Y - GameplayHudLayout.Margin - size.Y);
        return new Vector2(
            Math.Clamp(pos.X, GameplayHudLayout.Margin, maxX),
            Math.Clamp(pos.Y, GameplayHudLayout.Margin, maxY));
    }

    private static Rect2 GetCharacterTabRect(Rect2 panel, int index)
    {
        const float gap = 6f;
        var width = (panel.Size.X - 28f - gap * (CharacterPanelTabs.Length - 1)) / CharacterPanelTabs.Length;
        return new Rect2(panel.Position.X + 14f + index * (width + gap), panel.Position.Y + 40f, width, 24f);
    }

    private bool TryGetCharacterTabAtPosition(Vector2 localPos, Rect2 panel, out CharacterPanelTab tab)
    {
        for (var i = 0; i < CharacterPanelTabs.Length; i++)
        {
            if (GetCharacterTabRect(panel, i).HasPoint(localPos))
            {
                tab = CharacterPanelTabs[i];
                return true;
            }
        }
        tab = CharacterPanelTab.Resumen;
        return false;
    }

    private bool TryGetInventoryGridSlotAtPosition(Vector2 localPos, out int slot, out Rect2 rect)
    {
        slot = 0;
        rect = default;
        if (_world is null || !_inventoryOpen)
        {
            return false;
        }

        var layout = BuildLayout(AoUiFonts.Ui);
        var panel = GetInventoryPanelRect(layout);
        var gridPos = GetInventoryGridPosition(panel.Position);
        for (var i = 1; i <= _world.Inventory.UnlockedSlots; i++)
        {
            var col = (i - 1) % GameplayHudLayout.InvCols;
            var row = (i - 1) / GameplayHudLayout.InvCols;
            var pos = new Vector2(
                gridPos.X + col * (GameplayHudLayout.SlotSize + GameplayHudLayout.SlotGap),
                gridPos.Y + row * (GameplayHudLayout.SlotSize + GameplayHudLayout.SlotGap));
            rect = new Rect2(pos, new Vector2(GameplayHudLayout.SlotSize, GameplayHudLayout.SlotSize));
            if (rect.HasPoint(localPos))
            {
                slot = i;
                return true;
            }
        }

        rect = default;
        return false;
    }

    private bool TryGetEquipmentItemAtPosition(Vector2 localPos, out int slot, out Rect2 rect)
    {
        slot = 0;
        rect = default;
        if (_world is null || !_inventoryOpen)
        {
            return false;
        }

        var layout = BuildLayout(AoUiFonts.Ui);
        var panel = GetInventoryPanelRect(layout);
        var equipmentPos = panel.Position + new Vector2(14f, 38f);
        var equipmentWidth = panel.Size.X - 28f;
        for (var i = 0; i < EquipmentSlots.Length; i++)
        {
            rect = GetEquipmentSlotRect(equipmentPos, equipmentWidth, i);
            if (!rect.HasPoint(localPos))
            {
                continue;
            }

            slot = FindEquippedSlot(EquipmentSlots[i].ObjType);
            if (slot > 0)
            {
                return true;
            }
            rect = default;
            return false;
        }

        rect = default;
        return false;
    }

    private static Rect2 GetEquipmentSlotRect(Vector2 pos, float width, int index)
    {
        const float gap = 6f;
        var cellW = (width - 20f - gap * (EquipmentSlots.Length - 1)) / EquipmentSlots.Length;
        var y = pos.Y + 38f;
        return new Rect2(
            pos.X + 10f + index * (cellW + gap),
            y,
            cellW,
            56f);
    }

    private void DrawInventorySlot(Vector2 pos, int slot, int showKey)
    {
        var rect = new Rect2(pos, new Vector2(GameplayHudLayout.SlotSize, GameplayHudLayout.SlotSize));
        var unlocked = _world!.Inventory.IsSlotUnlocked(slot);
        DrawRect(rect, unlocked ? SlotFill : SlotLocked);
        DrawBorder(rect, SlotBorder);

        if (showKey > 0)
        {
            HudTextDraw.AtTopLeft(this, AoUiFonts.Ui, pos + new Vector2(3, 2), showKey.ToString(), 9, TextMuted);
        }

        if (!unlocked)
        {
            HudTextDraw.Centered(this, AoUiFonts.Ui, rect.GetCenter(), "×", 14, TextMuted);
            return;
        }

        var item = _world.Inventory.GetSlot(slot);
        if (item.IsEmpty)
        {
            return;
        }

        if (item.Equipped)
        {
            DrawBorder(rect.Grow(-1), SlotEquipped);
        }

        DrawItemIcon(item.ObjIndex, rect.Grow(-4), item.ElementalTags, item.Equipped);
        if (item.Amount > 1)
        {
            HudTextDraw.AtBottomRightInRect(this, AoUiFonts.Ui, rect, item.Amount.ToString(), 9, TextPrimary);
        }
    }

    private void DrawItemIcon(int objIndex, Rect2 dest, int elementalTags = 0, bool equipped = false)
    {
        var itemDef = _resources?.Items?.Get(objIndex);
        var grh = itemDef?.GrhIndex ?? _resources?.Objects.GetGrh(objIndex) ?? 0;
        if (grh <= 0 || _resources is null)
        {
            DrawRect(dest, new Color(0.25f, 0.28f, 0.35f));
            return;
        }
        var def = _resources.Grhs.ResolveDrawable(grh, (int)Time.GetTicksMsec());
        if (def is null || def.FileNum <= 0)
        {
            return;
        }
        var tex = _resources.Textures.Get(def.FileNum);
        if (tex is null)
        {
            return;
        }
        var fit = MathF.Min(dest.Size.X / def.PixelWidth, dest.Size.Y / def.PixelHeight);
        var scale = MathF.Min(1f, fit);
        var w = def.PixelWidth * scale;
        var h = def.PixelHeight * scale;
        var centered = new Rect2(
            dest.Position.X + (dest.Size.X - w) * 0.5f,
            dest.Position.Y + (dest.Size.Y - h) * 0.5f,
            w, h);
        var src = new Rect2(def.SX, def.SY, def.PixelWidth, def.PixelHeight);
        var objType = itemDef?.ObjType ?? _resources.Objects.Get(objIndex)?.ObjType ?? 0;
        var ticks = (float)Time.GetTicksMsec();
        var pulse = 0.5f + 0.5f * Mathf.Sin(ticks / 180.0f);
        if (elementalTags != 0)
        {
            var strength = equipped ? 0.92f : 0.74f;
            foreach (var layer in ItemAuraVisuals.EnumerateReplicaLayers(elementalTags, objType, pulse, ticks, strength))
            {
                var grow = layer.Grow * 0.38f;
                var offset = layer.Offset * 0.34f;
                var auraRect = new Rect2(centered.Position - new Vector2(grow, grow) + offset, centered.Size + new Vector2(grow * 2f, grow * 2f));
                DrawTextureRectRegion(tex, auraRect, src, layer.Color);
            }
        }
        DrawTextureRectRegion(tex, centered, src);
        if (elementalTags != 0)
        {
            foreach (var layer in ItemAuraVisuals.EnumerateHighlightLayers(elementalTags, objType, pulse, ticks, equipped ? 0.88f : 0.72f))
            {
                DrawTextureRectRegion(tex, new Rect2(centered.Position + layer.Offset * 0.28f, centered.Size), src, layer.Color);
            }
            ItemAuraVisuals.DrawSparkles(this, centered, elementalTags, objType, pulse, ticks, equipped ? 0.82f : 0.68f);
        }
    }

    private void DrawMapChip(Font font, GameplayHudLayout layout)
    {
        var mapName = _world!.Map?.Name;
        if (string.IsNullOrWhiteSpace(mapName))
        {
            mapName = $"Mapa {_world.MapId}";
        }
        var coords = $"({_world.TileX}, {_world.TileY})";
        var chipH = 44f;

        DrawPanel(layout.MapChipPos, new Vector2(layout.MapChipW, chipH));
        DrawAccent(layout.MapChipPos + new Vector2(12, 8), layout.MapChipW - 24);
        HudTextDraw.AtTopLeft(this, font, layout.MapChipPos + new Vector2(12, 12), mapName, 12, TextPrimary);
        HudTextDraw.AtTopLeft(this, font, layout.MapChipPos + new Vector2(12, 28), coords, 10, TextMuted);
    }

    private void DrawPanel(Vector2 pos, Vector2 size)
    {
        DrawRect(new Rect2(pos + new Vector2(2, 3), size), new Color(0, 0, 0, 0.35f));
        DrawRect(new Rect2(pos, size), PanelFill);
        DrawBorder(new Rect2(pos, size), PanelBorder);
    }

    private void DrawAccent(Vector2 pos, float width) =>
        DrawRect(new Rect2(pos, new Vector2(width, 1)), PanelHighlight);

    private void DrawBorder(Rect2 rect, Color color)
    {
        const float t = 1f;
        DrawRect(new Rect2(rect.Position, new Vector2(rect.Size.X, t)), color);
        DrawRect(new Rect2(rect.Position + new Vector2(0, rect.Size.Y - t), new Vector2(rect.Size.X, t)), color);
        DrawRect(new Rect2(rect.Position, new Vector2(t, rect.Size.Y)), color);
        DrawRect(new Rect2(rect.Position + new Vector2(rect.Size.X - t, 0), new Vector2(t, rect.Size.Y)), color);
    }

    private void DrawSectionCard(Font font, Rect2 rect, string title)
    {
        DrawRect(rect, new Color(0.07f, 0.08f, 0.12f, 0.92f));
        DrawBorder(rect, new Color(PanelBorder, 0.72f));
        DrawAccent(rect.Position + new Vector2(10f, 8f), rect.Size.X - 20f);
        HudTextDraw.AtTopLeft(this, font, rect.Position + new Vector2(10f, 14f), title, 12, TextPrimary);
    }

    private void DrawStatRows(Font font, Vector2 start, float rowGap, params StatLine[] rows)
    {
        for (var i = 0; i < rows.Length; i++)
        {
            HudTextDraw.AtTopLeft(this, font, start + new Vector2(0f, i * rowGap), rows[i].Label, 10, TextMuted);
            HudTextDraw.AtTopRight(this, font, start.X + 230f, start.Y + i * rowGap, rows[i].Value, 10, TextPrimary);
        }
    }

    private void DrawParagraph(Font font, Vector2 pos, float width, string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = "";
        var y = pos.Y;
        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
            if (font.GetStringSize(candidate, fontSize: 10).X > width && !string.IsNullOrEmpty(line))
            {
                HudTextDraw.AtTopLeft(this, font, new Vector2(pos.X, y), line, 10, TextPrimary);
                line = word;
                y += 16f;
            }
            else
            {
                line = candidate;
            }
        }
        if (!string.IsNullOrEmpty(line))
        {
            HudTextDraw.AtTopLeft(this, font, new Vector2(pos.X, y), line, 10, TextPrimary);
        }
    }

    private void DrawBigStat(Font font, Vector2 pos, string label, int current, int max, Color accent)
    {
        DrawRect(new Rect2(pos, new Vector2(230f, 64f)), new Color(0.09f, 0.1f, 0.14f, 0.95f));
        DrawBorder(new Rect2(pos, new Vector2(230f, 64f)), new Color(PanelBorder, 0.6f));
        HudTextDraw.AtTopLeft(this, font, pos + new Vector2(12f, 10f), label, 10, TextMuted);
        HudTextDraw.AtTopLeft(this, font, pos + new Vector2(12f, 28f), $"{current} / {max}", 15, TextPrimary);
        var ratio = max > 0 ? Math.Clamp(current / (float)max, 0f, 1f) : 0f;
        DrawRect(new Rect2(pos + new Vector2(12f, 50f), new Vector2(206f, 4f)), new Color(0.08f, 0.08f, 0.1f, 1f));
        DrawRect(new Rect2(pos + new Vector2(12f, 50f), new Vector2(206f * ratio, 4f)), accent);
    }

    private void DrawEquipmentRow(Font font, Rect2 rect, EquippedItemRow row)
    {
        DrawRect(rect, new Color(0.09f, 0.1f, 0.14f, 0.95f));
        DrawBorder(rect, new Color(PanelBorder, 0.62f));
        HudTextDraw.AtTopLeft(this, font, rect.Position + new Vector2(10f, 8f), row.SlotLabel, 10, TextMuted);
        HudTextDraw.AtTopLeft(this, font, rect.Position + new Vector2(10f, 24f), row.Name, 11, row.Color);
        HudTextDraw.AtTopLeft(this, font, rect.Position + new Vector2(10f, 40f), row.Detail, 9, TextMuted);
    }

    private static StatLine Row(string label, string value) => new(label, value);

    private string SafeName() => string.IsNullOrWhiteSpace(_world?.CharacterName) ? "Aventurero" : _world.CharacterName;

    private string ClassLabel(int classId) => classId switch
    {
        1 => "Mago",
        2 => "Clérigo",
        3 => "Guerrero",
        4 => "Asesino",
        5 => "Bardo",
        6 => "Druida",
        7 => "Paladín",
        8 => "Cazador",
        9 => "Trabajador",
        10 => "Pirata",
        11 => "Ladrón",
        12 => "Bandido",
        _ => $"Clase {classId}",
    };

    private string MapLabel()
    {
        var mapName = _world?.Map?.Name;
        return string.IsNullOrWhiteSpace(mapName) ? $"Mapa {_world?.MapId}" : mapName;
    }

    private string CharacterStateLabel()
    {
        if (_world!.IsDead) return "Muerto";
        if (_world.Hidden) return "Oculto";
        if (_world.Meditating) return "Meditando";
        return "Activo";
    }

    private static string BoolLabel(bool value) => value ? "Sí" : "No";

    private int ComputeAttackPower()
    {
        var weaponGfx = _world?.GfxWeapon ?? 0;
        return _world is null ? 0 : _world.Level * 3 + 15 + Math.Max(0, weaponGfx / 4);
    }

    private IEnumerable<EquippedItemRow> EnumerateEquippedItemRows()
    {
        foreach (var slot in EquipmentSlots)
        {
            var invSlot = FindEquippedSlot(slot.ObjType);
            if (invSlot <= 0)
            {
                yield return new EquippedItemRow(slot.Label, "Sin equipar", "", TextPrimary);
                continue;
            }
            var item = _world!.Inventory.GetSlot(invSlot);
            var def = _resources?.Items?.Get(item.ObjIndex);
            var name = ItemAffixes.BuildDisplayName(def, item.ElementalTags);
            var lvl = ItemAffixes.RequiredLevel(def, item.ElementalTags);
            var detail = $"Nivel req. {lvl}";
            if (item.ElementalTags != 0)
            {
                detail += $" · {ItemAffixes.EffectCount(def, item.ElementalTags)} efecto(s)";
            }
            yield return new EquippedItemRow(slot.Label, name, detail, ItemAffixes.TitleColor(item.ElementalTags));
        }
    }

    private static string CharacterPanelLabel(CharacterPanelTab tab) => tab switch
    {
        CharacterPanelTab.Resumen => "Resumen",
        CharacterPanelTab.Recursos => "Recursos",
        CharacterPanelTab.Combate => "Combate",
        CharacterPanelTab.Equipo => "Equipo",
        CharacterPanelTab.Mundo => "Mundo",
        _ => tab.ToString(),
    };

    private static string FormatNumber(int value) =>
        value.ToString("N0", CultureInfo.GetCultureInfo("es-AR"));

    private readonly record struct EquipmentSlot(string Label, int ObjType);
    private readonly record struct StatLine(string Label, string Value);
    private readonly record struct EquippedItemRow(string SlotLabel, string Name, string Detail, Color Color);

    private static readonly CharacterPanelTab[] CharacterPanelTabs =
    [
        CharacterPanelTab.Resumen,
        CharacterPanelTab.Recursos,
        CharacterPanelTab.Combate,
        CharacterPanelTab.Equipo,
        CharacterPanelTab.Mundo,
    ];
}

public enum CharacterPanelTab
{
    Resumen,
    Recursos,
    Combate,
    Equipo,
    Mundo,
}
