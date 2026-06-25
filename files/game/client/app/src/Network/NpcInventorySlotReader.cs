using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

/// <summary>VB6 Protocol.HandleChangeNPCInventorySlot.</summary>
public static class NpcInventorySlotReader
{
    public readonly struct SlotData
    {
        public byte Slot { get; init; }
        public short ObjIndex { get; init; }
        public short Amount { get; init; }
        public float Price { get; init; }
    }

    public static SlotData Parse(ref LegacyPacketReader reader)
    {
        var slot = reader.ReadUInt8();
        var objIndex = reader.ReadInt16();
        var amount = reader.ReadInt16();
        var price = reader.ReadReal32();
        _ = reader.ReadInt32(); // elementalTags
        _ = reader.ReadUInt8(); // puedeUsar
        return new SlotData
        {
            Slot = slot,
            ObjIndex = objIndex,
            Amount = amount,
            Price = price,
        };
    }

    public static void Apply(in SlotData data, NpcCommerceInventory inventory) =>
        inventory.SetSlot(data.Slot, data.ObjIndex, data.Amount, data.Price);
}
