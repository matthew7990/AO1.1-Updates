using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

/// <summary>VB6 Protocol.HandleChangeInventorySlot.</summary>
public static class InventorySlotReader
{
    public static void Apply(ref LegacyPacketReader reader, PlayerInventory inventory)
    {
        var slot = reader.ReadUInt8();
        var objIndex = reader.ReadInt16();
        var amount = reader.ReadInt16();
        var equipped = reader.ReadBoolean();
        _ = reader.ReadReal32();
        _ = reader.ReadUInt8();
        var elementalTags = reader.ReadInt32();
        _ = reader.ReadBoolean();
        inventory.SetSlot(slot, objIndex, amount, equipped, elementalTags);
    }
}
