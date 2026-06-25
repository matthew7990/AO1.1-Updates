namespace Argentum.Client.World;

/// <summary>VB6 UserHechizos[1..40].</summary>
public sealed class SpellBook
{
    public const int MaxSlots = 40;

    private readonly short[] _slots = new short[MaxSlots + 1];

    public short GetSlot(int slot) =>
        slot is >= 1 and <= MaxSlots ? _slots[slot] : (short)0;

    public void SetSlot(int slot, short spellId)
    {
        if (slot is >= 1 and <= MaxSlots)
        {
            _slots[slot] = spellId;
        }
    }

    public string SlotLabel(int slot, Resources.SpellCatalog? catalog)
    {
        var id = GetSlot(slot);
        if (id <= 0)
        {
            return "—";
        }
        return catalog?.Get(id)?.Name ?? $"Hechizo {id}";
    }
}
