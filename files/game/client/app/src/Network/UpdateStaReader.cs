using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

public static class UpdateStaReader
{
    public static void Apply(ref LegacyPacketReader reader, WorldSession session)
    {
        session.MinSta = reader.ReadInt16();
    }
}
