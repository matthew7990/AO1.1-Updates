using Argentum.Client.Audio;
using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

/// <summary>VB6 HandleCharSwing — miss al atacar (eCharSwing 19).</summary>
public static class CharSwingReader
{
    public static void Apply(ref LegacyPacketReader reader, WorldSession session, WorldCharacters characters, AoAudio? audio)
    {
        var charIndex = reader.ReadInt16();
        _ = reader.ReadBoolean(); // showFx
        _ = reader.ReadBoolean(); // showText
        _ = reader.ReadBoolean(); // notifyText
        if (charIndex != session.CharIndex && characters.TryGet(charIndex, out var ch))
        {
            ch!.Motion.TriggerWeaponShieldAttack();
        }
        var srcX = session.TileX;
        var srcY = session.TileY;
        if (charIndex != session.CharIndex && characters.TryGet(charIndex, out var other))
        {
            srcX = other!.TileX;
            srcY = other.TileY;
        }
        audio?.PlayWave(AoSoundIndex.SwingMiss, srcX, srcY);
    }
}
