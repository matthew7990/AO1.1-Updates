using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

/// <summary>VB6 Protocol.HandleBlockPosition.</summary>
public static class BlockPositionReader
{
    private const byte AllSides = 15;

    public static void Apply(int x, int y, byte blocked, WorldSession world)
    {
        if (x < CsmMap.MinMapTile || y < CsmMap.MinMapTile
            || x > CsmMap.MaxMapTile || y > CsmMap.MaxMapTile)
        {
            return;
        }
        byte merged = (byte)(blocked & AllSides);
        if (world.Map is not null)
        {
            var tile = world.Map.Tiles[x, y];
            merged = (byte)((tile.Blocked & ~AllSides) | (blocked & AllSides));
            tile.Blocked = merged;
        }
        world.Blocks[(x, y)] = merged;
    }

    public static void Apply(ref LegacyPacketReader reader, WorldSession world)
    {
        Apply(reader.ReadUInt8(), reader.ReadUInt8(), reader.ReadUInt8(), world);
    }
}
