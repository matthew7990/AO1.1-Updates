using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

public static class ChangeSpellSlotReader
{
    public static void Apply(ref LegacyPacketReader reader, SpellBook book)
    {
        var slot = reader.ReadUInt8();
        var spellId = reader.ReadInt16();
        var index = reader.ReadInt16();
        _ = reader.ReadBoolean();
        if (slot is < 1 or > SpellBook.MaxSlots)
        {
            return;
        }
        if (index < 0 || spellId <= 0)
        {
            book.SetSlot(slot, 0);
        }
        else
        {
            book.SetSlot(slot, spellId);
        }
    }
}
