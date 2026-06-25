using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

/// <summary>VB6 HandleCharacterChange — eCharacterChange (49).</summary>
public static class CharacterChangeReader
{
    public const int CasperBodyIdle = 829;

    public static void Apply(ref LegacyPacketReader reader, WorldSession session, WorldCharacters characters)
    {
        var charIndex = reader.ReadInt16();
        var flags = reader.ReadUInt8();
        _ = flags;
        var body = reader.ReadInt16();
        var head = reader.ReadInt16();
        var heading = reader.ReadUInt8();
        var weapon = reader.ReadInt16();
        var shield = reader.ReadInt16();
        var helmet = reader.ReadInt16();
        _ = reader.ReadInt16(); // cart
        _ = reader.ReadInt16(); // backpack
        _ = reader.ReadInt16(); // fx
        _ = reader.ReadInt16(); // fx loops

        if (charIndex == session.CharIndex)
        {
            ApplyToSession(session, body, head, heading, weapon, shield, helmet);
            NetDiagnostics.Log("CharacterChange", $"self body={body} head={head} dead={body == CasperBodyIdle}");
            return;
        }
        if (characters.TryGet(charIndex, out var ch))
        {
            ch!.Body = body;
            ch.Head = head;
            ch.Heading = heading;
            ch.Weapon = weapon;
            ch.Shield = shield;
            ch.Helmet = helmet;
        }
    }

    private static void ApplyToSession(WorldSession session, int body, int head, int heading, int weapon, int shield, int helmet)
    {
        session.Body = body;
        session.Head = head;
        session.Heading = heading;
        session.GfxWeapon = weapon;
        session.GfxShield = shield;
        session.GfxHelmet = helmet;
        session.IsDead = body == CasperBodyIdle;
        if (session.IsDead)
        {
            session.ApplyDeathAppearance();
        }
        else
        {
            session.DeathMessage = null;
        }
    }
}
