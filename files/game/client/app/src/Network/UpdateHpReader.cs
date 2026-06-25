using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

/// <summary>VB6 HandleUpdateHP — eUpdateHP (27).</summary>
public static class UpdateHpReader
{
    public static void Apply(ref LegacyPacketReader reader, WorldSession session)
    {
        session.MinHp = reader.ReadInt16();
        _ = reader.ReadInt32(); // shield
        var wasDead = session.IsDead;
        session.IsDead = session.MinHp <= 0;
        if (session.IsDead)
        {
            session.ApplyDeathAppearance();
            if (!wasDead)
            {
                NetDiagnostics.Log("UpdateHP", $"muerto hp=0 body={session.Body}");
            }
        }
        else
        {
            session.DeathMessage = null;
        }
    }
}
