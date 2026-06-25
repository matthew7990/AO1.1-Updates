using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

public static class ArmaMovReader
{
    public static void Apply(ref LegacyPacketReader reader, WorldSession session, WorldCharacters characters)
    {
        var charIndex = reader.ReadInt16();
        _ = reader.ReadUInt8(); // isRanged
        if (charIndex == session.CharIndex)
        {
            session.Motion.TriggerWeaponShieldAttack();
            return;
        }
        if (characters.TryGet(charIndex, out var ch))
        {
            ch!.Motion.TriggerWeaponShieldAttack();
        }
    }
}
