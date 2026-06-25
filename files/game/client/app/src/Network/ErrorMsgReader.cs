using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

public static class ErrorMsgReader
{
    public static string Read(ref LegacyPacketReader reader)
    {
        return reader.ReadString8();
    }
}
