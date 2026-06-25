using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

/// <summary>VB6 HandleNPCHitUser — eNPCHitUser (32).</summary>
public static class NpcHitUserReader
{
    public static void Apply(ref LegacyPacketReader reader, WorldSession session)
    {
        var bodyPart = reader.ReadUInt8();
        var damage = reader.ReadInt16();
        _ = bodyPart;
        _ = damage;
        NetDiagnostics.Log("NPCHitUser", $"part={bodyPart} dmg={damage} hp={session.MinHp}");
    }
}
