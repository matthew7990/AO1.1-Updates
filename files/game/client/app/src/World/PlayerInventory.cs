namespace Argentum.Client.World;

/// <summary>Inventario del usuario — slots 1..42 (VB6 MAX_INVENTORY_SLOTS).</summary>
public sealed class PlayerInventory
{
    public const int MaxSlots = 42;
    public const int HotbarSlots = 10;
    public const int DefaultUnlocked = 24;

    public int UnlockedSlots { get; set; } = DefaultUnlocked;

    private readonly InventorySlot[] _slots = new InventorySlot[MaxSlots + 1];

    public void SetSlot(int slot, int objIndex, int amount, bool equipped, int elementalTags = 0)
    {
        if (slot < 1 || slot > MaxSlots)
        {
            return;
        }
        _slots[slot] = new InventorySlot
        {
            ObjIndex = objIndex,
            Amount = amount,
            Equipped = equipped,
            ElementalTags = elementalTags,
        };
    }

    public InventorySlot GetSlot(int slot)
    {
        if (slot < 1 || slot > MaxSlots)
        {
            return default;
        }
        return _slots[slot];
    }

    public bool IsSlotUnlocked(int slot) => slot >= 1 && slot <= UnlockedSlots;

    public readonly struct InventorySlot
    {
        public int ObjIndex { get; init; }
        public int Amount { get; init; }
        public bool Equipped { get; init; }
        public int ElementalTags { get; init; }
        public bool IsEmpty => ObjIndex <= 0;
    }
}
