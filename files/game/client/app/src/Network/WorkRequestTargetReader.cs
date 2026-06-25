using Argentum.Client.Protocol;

namespace Argentum.Client.Network;

public static class WorkRequestTargetReader
{
    public readonly struct Data
    {
        public int Skill { get; init; }
        public bool Area { get; init; }
        public int Radio { get; init; }
    }

    public static Data Parse(ref LegacyPacketReader reader) => new()
    {
        Skill = reader.ReadUInt8(),
        Area = reader.ReadBoolean(),
        Radio = reader.ReadUInt8(),
    };
}
