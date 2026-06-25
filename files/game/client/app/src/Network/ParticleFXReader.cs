using Argentum.Client.Protocol;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Network;

public static class ParticleFXReader
{
    public static void ApplyWithDestino(ref LegacyPacketReader reader, WorldSession world, WorldCharacters characters)
    {
        var emisor = reader.ReadInt16();
        var receptor = reader.ReadInt16();
        _ = reader.ReadInt16(); // particulaViaje
        var particulaFinal = reader.ReadInt16();
        _ = reader.ReadInt32(); // timeMs
        _ = reader.ReadInt16(); // wav
        var fx = reader.ReadInt16();
        _ = reader.ReadUInt8();
        _ = reader.ReadUInt8();
        ApplyImpactFx(world, characters, emisor, receptor, particulaFinal, fx);
    }

    public static void ApplyWithDestinoXY(ref LegacyPacketReader reader, WorldSession world, WorldCharacters characters)
    {
        var emisor = reader.ReadInt16();
        _ = reader.ReadInt16(); // particulaViaje
        var particulaFinal = reader.ReadInt16();
        _ = reader.ReadInt32(); // timeMs
        _ = reader.ReadInt16(); // wav
        var fx = reader.ReadInt16();
        var tileX = reader.ReadUInt8();
        var tileY = reader.ReadUInt8();
        var receptor = characters.CharIndexAt(tileX, tileY);
        if (receptor <= 0)
        {
            receptor = emisor;
        }
        ApplyImpactFx(world, characters, emisor, receptor, particulaFinal, fx);
    }

    private static void ApplyImpactFx(
        WorldSession world,
        WorldCharacters characters,
        short emisor,
        short receptor,
        short particulaFinal,
        short fxFlag)
    {
        if (particulaFinal <= 0)
        {
            return;
        }
        var loops = fxFlag > 0 ? (short)1 : (short)3;
        var now = (long)Time.GetTicksMsec();
        if (receptor == world.CharIndex)
        {
            world.SelfFx.Set(particulaFinal, loops, now);
            return;
        }
        if (characters.TryGet(receptor, out var ch))
        {
            ch!.Fx.Set(particulaFinal, loops, now);
            return;
        }
        if (emisor == world.CharIndex)
        {
            world.SelfFx.Set(particulaFinal, loops, now);
            return;
        }
        if (characters.TryGet(emisor, out var caster))
        {
            caster!.Fx.Set(particulaFinal, loops, now);
        }
    }
}
