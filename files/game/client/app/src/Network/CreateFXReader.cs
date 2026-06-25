using Argentum.Client.Protocol;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Network;

public static class CreateFXReader
{
    public static void Apply(ref LegacyPacketReader reader, WorldSession world, WorldCharacters characters)
    {
        var charIndex = reader.ReadInt16();
        var fx = reader.ReadInt16();
        var loops = reader.ReadInt16();
        _ = reader.ReadUInt8();
        _ = reader.ReadUInt8();
        var now = (long)Time.GetTicksMsec();
        if (charIndex == world.CharIndex)
        {
            world.SelfFx.Set(fx, loops, now);
            return;
        }
        if (characters.TryGet(charIndex, out var ch))
        {
            ch!.Fx.Set(fx, loops, now);
        }
    }
}
