using System;
using System.Globalization;
using System.Threading.Tasks;
using Argentum.Client.Core;
using Argentum.Client.Resources;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>Equivalente funcional de frmComerciar (VB6).</summary>
public partial class CommerceScreen : Control
{
    private static readonly Color PanelFill = new(0.05f, 0.06f, 0.1f, 0.96f);
    private static readonly Color PanelBorder = new(0.68f, 0.58f, 0.36f, 0.75f);
    private static readonly Color TextPrimary = new("ebe4d6");
    private static readonly Color TextMuted = new(0.58f, 0.56f, 0.52f);
    private static readonly Color SlotFill = new(0.1f, 0.11f, 0.16f, 0.95f);
    private static readonly Color SlotBorder = new(0.45f, 0.4f, 0.32f, 0.7f);
    private static readonly Color SlotSelected = new(0.75f, 0.62f, 0.28f, 0.85f);

    private WorldSession? _world;
    private GameResources? _resources;
    private int _selectedNpcSlot;
    private int _selectedUserSlot;
    private int _quantity = 1;
    private Func<int, int, Task>? _buyAsync;
    private Func<int, int, Task>? _sellAsync;
    private Func<Task>? _endAsync;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 20;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public void Bind(
        WorldSession? world,
        GameResources? resources,
        Func<int, int, Task>? buyAsync,
        Func<int, int, Task>? sellAsync,
        Func<Task>? endAsync)
    {
        _world = world;
        _resources = resources;
        _buyAsync = buyAsync;
        _sellAsync = sellAsync;
        _endAsync = endAsync;
    }

    public void Open(string vendorName)
    {
        _selectedNpcSlot = 0;
        _selectedUserSlot = 0;
        _quantity = 1;
        MouseFilter = MouseFilterEnum.Stop;
        Visible = true;
        _world!.CommerceOpen = true;
        _world.CommerceVendor = vendorName;
        QueueRedraw();
    }

    public void Close()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        if (_world is not null)
        {
            _world.CommerceOpen = false;
            _world.NpcCommerce.Clear();
        }
        QueueRedraw();
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible)
        {
            return;
        }
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            _ = _endAsync?.Invoke();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Draw()
    {
        if (!Visible || _world is null)
        {
            return;
        }

        var size = GameViewport.GetRenderSize(GetViewport());
        if (size.X < 1 || size.Y < 1)
        {
            size = GetViewportRect().Size;
        }
        var panelW = Math.Min(720f, size.X - 40f);
        var panelH = Math.Min(420f, size.Y - 80f);
        var px = (size.X - panelW) * 0.5f;
        var py = (size.Y - panelH) * 0.5f;
        var rect = new Rect2(px, py, panelW, panelH);
        DrawRect(rect, PanelFill);
        DrawRect(rect, PanelBorder, false, 2f);

        var font = AoUiFonts.Ui;
        var title = string.IsNullOrEmpty(_world.CommerceVendor)
            ? "Comerciar"
            : $"Comerciar con {_world.CommerceVendor}";
        HudTextDraw.AtTopCenter(this, font, px + panelW * 0.5f, py + 10f, title, 14, TextPrimary);
        HudTextDraw.AtTopCenter(this, font, px + panelW * 0.5f, py + 30f,
            $"Oro: {_world.Gold:N0}", 11, TextMuted);

        var cols = 7;
        var slotSize = 40f;
        var gap = 4f;
        var gridW = cols * (slotSize + gap);
        var leftX = px + (panelW - gridW * 2f - 24f) * 0.5f;
        var gridY = py + 56f;

        HudTextDraw.AtTopLeft(this, font, new Vector2(leftX, gridY - 18f), "NPC", 10, TextMuted);
        DrawCommerceGrid(leftX, gridY, cols, slotSize, gap, true);
        var rightX = leftX + gridW + 24f;
        HudTextDraw.AtTopLeft(this, font, new Vector2(rightX, gridY - 18f), "Tu inventario", 10, TextMuted);
        DrawCommerceGrid(rightX, gridY, cols, slotSize, gap, false);

        var btnY = py + panelH - 44f;
        DrawButton(font, px + panelW * 0.5f - 160f, btnY, 70f, "Comprar", _selectedNpcSlot > 0);
        DrawButton(font, px + panelW * 0.5f - 70f, btnY, 70f, "Vender", _selectedUserSlot > 0);
        DrawButton(font, px + panelW * 0.5f + 20f, btnY, 70f, "Cerrar", true);
        HudTextDraw.AtTopCenter(this, font, px + panelW * 0.5f, btnY - 18f,
            $"Cantidad: {_quantity} (+/-)", 10, TextMuted);
        DrawSelectedItemDetails(font, px, py, panelW, gridY, cols, slotSize, gap);
    }

    private void DrawSelectedItemDetails(Font font, float px, float py, float panelW, float gridY, int cols, float slotSize, float gap)
    {
        var detailY = gridY + 6 * (slotSize + gap) + 8f;
        if (_selectedNpcSlot > 0)
        {
            var slot = _world!.NpcCommerce.GetSlot(_selectedNpcSlot);
            if (slot.ObjIndex > 0)
            {
                DrawItemDetails(font, px + 16f, detailY, panelW - 32f, slot.ObjIndex, slot.Price, true);
            }
            return;
        }
        if (_selectedUserSlot > 0)
        {
            var slot = _world!.Inventory.GetSlot(_selectedUserSlot);
            if (slot.ObjIndex > 0)
            {
                var saleUnit = MathF.Floor((_resources?.Items?.GetValor(slot.ObjIndex) ?? 0) / 3f);
                DrawItemDetails(font, px + 16f, detailY, panelW - 32f, slot.ObjIndex, saleUnit, false);
            }
        }
    }

    private void DrawItemDetails(Font font, float x, float y, float width, int objIndex, float unitPrice, bool buying)
    {
        var name = _resources?.Items?.GetName(objIndex) ?? $"Obj {objIndex}";
        var texto = _resources?.Items?.GetTexto(objIndex) ?? "";
        var total = (int)MathF.Floor(unitPrice * _quantity);
        var priceLabel = buying ? $"Precio: {total:N0}" : $"Venta: {total:N0}";
        HudTextDraw.AtTopLeft(this, font, new Vector2(x, y), name, 12, TextPrimary);
        if (!string.IsNullOrWhiteSpace(texto))
        {
            HudTextDraw.AtTopLeft(this, font, new Vector2(x, y + 16f), Truncate(texto, 72), 10, TextMuted);
        }
        HudTextDraw.AtTopRight(this, font, x + width, y, priceLabel, 11, TextPrimary);
    }

    private static string Truncate(string text, int maxChars)
    {
        if (text.Length <= maxChars)
        {
            return text;
        }
        return text[..(maxChars - 3)] + "...";
    }

    private void DrawCommerceGrid(float x, float y, int cols, float slotSize, float gap, bool npc)
    {
        for (var slot = 1; slot <= NpcCommerceInventory.MaxSlots; slot++)
        {
            var col = (slot - 1) % cols;
            var row = (slot - 1) / cols;
            var sx = x + col * (slotSize + gap);
            var sy = y + row * (slotSize + gap);
            var rect = new Rect2(sx, sy, slotSize, slotSize);
            var selected = npc ? slot == _selectedNpcSlot : slot == _selectedUserSlot;
            DrawRect(rect, SlotFill);
            DrawRect(rect, selected ? SlotSelected : SlotBorder, false, selected ? 2f : 1f);

            int objIndex;
            int amount;
            if (npc)
            {
                var s = _world!.NpcCommerce.GetSlot(slot);
                objIndex = s.ObjIndex;
                amount = s.Amount;
            }
            else
            {
                if (!_world!.Inventory.IsSlotUnlocked(slot))
                {
                    continue;
                }
                var s = _world.Inventory.GetSlot(slot);
                objIndex = s.ObjIndex;
                amount = s.Amount;
            }
            if (objIndex <= 0)
            {
                continue;
            }
            var grh = _resources?.Items?.GetGrh(objIndex) ?? 0;
            DrawItemIcon(grh, rect.Grow(-4));
            if (amount > 1)
            {
                HudTextDraw.AtBottomRightInRect(this, AoUiFonts.Ui, rect, amount.ToString(CultureInfo.InvariantCulture), 8, TextPrimary);
            }
        }
    }

    private void DrawButton(Font font, float x, float y, float w, string label, bool enabled)
    {
        var rect = new Rect2(x, y, w, 28f);
        DrawRect(rect, enabled ? SlotFill : new Color(0.08f, 0.08f, 0.1f, 0.9f));
        DrawRect(rect, PanelBorder, false, 1f);
        HudTextDraw.AtTopCenter(this, font, x + w * 0.5f, y + 6f, label, 10, enabled ? TextPrimary : TextMuted);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Visible || _world is null)
        {
            return;
        }
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            return;
        }
        var pos = click.Position;
        var size = GameViewport.GetRenderSize(GetViewport());
        var panelW = Math.Min(720f, size.X - 40f);
        var panelH = Math.Min(420f, size.Y - 80f);
        var px = (size.X - panelW) * 0.5f;
        var py = (size.Y - panelH) * 0.5f;
        var cols = 7;
        var slotSize = 40f;
        var gap = 4f;
        var gridW = cols * (slotSize + gap);
        var leftX = px + (panelW - gridW * 2f - 24f) * 0.5f;
        var gridY = py + 56f;
        var rightX = leftX + gridW + 24f;

        if (TryHitGrid(pos, leftX, gridY, cols, slotSize, gap, out var hitSlot))
        {
            _selectedNpcSlot = hitSlot;
            _selectedUserSlot = 0;
            QueueRedraw();
            AcceptEvent();
            return;
        }
        if (TryHitGrid(pos, rightX, gridY, cols, slotSize, gap, out hitSlot))
        {
            _selectedUserSlot = hitSlot;
            _selectedNpcSlot = 0;
            QueueRedraw();
            AcceptEvent();
            return;
        }

        var btnY = py + panelH - 44f;
        if (new Rect2(px + panelW * 0.5f - 160f, btnY, 70f, 28f).HasPoint(pos) && _selectedNpcSlot > 0)
        {
            _ = _buyAsync?.Invoke(_selectedNpcSlot, _quantity);
            AcceptEvent();
            return;
        }
        if (new Rect2(px + panelW * 0.5f - 70f, btnY, 70f, 28f).HasPoint(pos) && _selectedUserSlot > 0)
        {
            _ = _sellAsync?.Invoke(_selectedUserSlot, _quantity);
            AcceptEvent();
            return;
        }
        if (new Rect2(px + panelW * 0.5f + 20f, btnY, 70f, 28f).HasPoint(pos))
        {
            _ = _endAsync?.Invoke();
            AcceptEvent();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible)
        {
            return;
        }
        if (@event is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.Keycode == Key.Equal || key.Keycode == Key.KpAdd)
            {
                _quantity = Math.Min(10000, _quantity + 1);
                QueueRedraw();
            }
            else if (key.Keycode == Key.Minus || key.Keycode == Key.KpSubtract)
            {
                _quantity = Math.Max(1, _quantity - 1);
                QueueRedraw();
            }
        }
    }

    private static bool TryHitGrid(Vector2 pos, float x, float y, int cols, float slotSize, float gap, out int slot)
    {
        for (var s = 1; s <= NpcCommerceInventory.MaxSlots; s++)
        {
            var col = (s - 1) % cols;
            var row = (s - 1) / cols;
            var rect = new Rect2(x + col * (slotSize + gap), y + row * (slotSize + gap), slotSize, slotSize);
            if (rect.HasPoint(pos))
            {
                slot = s;
                return true;
            }
        }
        slot = 0;
        return false;
    }

    private void DrawItemIcon(int grh, Rect2 dest)
    {
        if (grh <= 0 || _resources is null)
        {
            DrawRect(dest, new Color(0.25f, 0.28f, 0.35f));
            return;
        }
        var def = _resources.Grhs.ResolveDrawable(grh, (int)Time.GetTicksMsec());
        if (def is null || def.FileNum <= 0)
        {
            DrawRect(dest, new Color(0.25f, 0.28f, 0.35f));
            return;
        }
        var tex = _resources.Textures.Get(def.FileNum);
        if (tex is null)
        {
            DrawRect(dest, new Color(0.25f, 0.28f, 0.35f));
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
}
