using Argentum.Client.World;

namespace Argentum.Client.Network;

/// <summary>VB6 HandleNPCKillUser — eNPCKillUser (16).</summary>
public static class NpcKillUserReader
{
    public static void Apply(WorldSession session)
    {
        session.IsDead = true;
        session.DeathMessage = "Una criatura te ha matado.";
        session.ApplyDeathAppearance();
        NetDiagnostics.Log("NPCKillUser", $"body={session.Body} alive={session.AliveBody}");
    }
}
