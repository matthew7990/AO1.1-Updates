namespace Argentum.Client.World;

/// <summary>Inventario del NPC en frmComerciar (VB6 InvComNpc).</summary>
public sealed class NpcCommerceInventory
{
    public const int MaxSlots = 42;

    private readonly CommerceSlot[] _slots = new CommerceSlot[MaxSlots + 1];

    public void Clear()
    {
        for (var i = 0; i < _slots.Length; i++)
        {
            _slots[i] = default;
        }
    }

    public void SetSlot(int slot, int objIndex, int amount, float price)
    {
        if (slot < 1 || slot > MaxSlots)
        {
            return;
        }
        _slots[slot] = new CommerceSlot
        {
            ObjIndex = objIndex,
            Amount = amount,
            Price = price,
        };
    }

    public CommerceSlot GetSlot(int slot)
    {
        if (slot < 1 || slot > MaxSlots)
        {
            return default;
        }
        return _slots[slot];
    }

    public readonly struct CommerceSlot
    {
        public int ObjIndex { get; init; }
        public int Amount { get; init; }
        public float Price { get; init; }
        public bool IsEmpty => ObjIndex <= 0;
    }
}
