using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

/// <summary>VB6 Protocol.HandleObjectCreate.</summary>
public static class ObjectCreateReader
{
    public static void Apply(int x, int y, int objIndex, int amount, int elementalTags, WorldSession world)
    {
        if (world.Map is null)
        {
            return;
        }
        if (x < CsmMap.MinMapTile || y < CsmMap.MinMapTile || x > CsmMap.MaxMapTile || y > CsmMap.MaxMapTile)
        {
            return;
        }
        var tile = world.Map.Tiles[x, y];
        tile.ObjectIndex = objIndex;
        tile.ObjectAmount = amount;
        tile.ObjectElementalTags = elementalTags;
        tile.ObjectIsDroppedItem = objIndex > 0;
    }

    public static void Apply(ref LegacyPacketReader reader, WorldSession world)
    {
        var x = reader.ReadUInt8();
        var y = reader.ReadUInt8();
        var objIndex = reader.ReadInt16();
        var amount = reader.ReadInt16();
        var elementalTags = reader.ReadInt32();
        Apply(x, y, objIndex, amount, elementalTags, world);
    }
}
