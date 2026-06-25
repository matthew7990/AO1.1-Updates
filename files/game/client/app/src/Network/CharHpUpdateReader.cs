using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

/// <summary>VB6 HandleCharUpdateHP — eCharUpdateHP (150).</summary>
public static class CharHpUpdateReader
{
    public static void Apply(ref LegacyPacketReader reader, WorldCharacters characters)
    {
        var charIndex = reader.ReadInt16();
        var minHp = reader.ReadInt32();
        var maxHp = reader.ReadInt32();
        var shield = reader.ReadInt32();
        if (characters.TryGet(charIndex, out var ch))
        {
            ch!.ApplyHpUpdate(minHp, maxHp, shield);
        }
    }
}
