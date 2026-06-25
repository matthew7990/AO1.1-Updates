using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

public static class CharacterCreateReader
{
    public static void ReadInto(ref LegacyPacketReader reader, WorldCharacter target)
    {
        target.CharIndex = reader.ReadInt16();
        target.Body = reader.ReadInt16();
        target.Head = reader.ReadInt16();
        target.Heading = reader.ReadUInt8();
        target.TileX = reader.ReadUInt8();
        target.TileY = reader.ReadUInt8();
        target.Weapon = reader.ReadInt16();
        target.Shield = reader.ReadInt16();
        target.Helmet = reader.ReadInt16();
        _ = reader.ReadInt16(); // cart
        _ = reader.ReadInt16(); // backpack
        _ = reader.ReadInt16(); // fx
        _ = reader.ReadInt16(); // fx loops
        target.Name = reader.ReadString8();
        _ = reader.ReadUInt8(); // status
        target.Privilege = PrivilegeFromMask(reader.ReadUInt8());
        _ = reader.ReadUInt8(); // particula
        for (var i = 0; i < 7; i++)
        {
            _ = reader.ReadString8();
        }
        _ = reader.ReadReal32(); // speeding
        var flagNpc = reader.ReadUInt8();
        target.IsNpc = flagNpc > 0;
        _ = reader.ReadUInt8(); // appear
        _ = reader.ReadInt16(); // group
        _ = reader.ReadInt16(); // clan
        _ = reader.ReadUInt8(); // clan nivel
        target.MinHp = reader.ReadInt32();
        target.MaxHp = reader.ReadInt32();
        _ = reader.ReadInt32(); // min mana
        _ = reader.ReadInt32(); // max mana
        _ = reader.ReadUInt8(); // simbolo
        _ = reader.ReadUInt8(); // flags
        _ = reader.ReadUInt8(); // tipo usuario
        _ = reader.ReadUInt8(); // team
        _ = reader.ReadUInt8(); // bandera
        target.NpcNumber = reader.ReadInt16();
        target.Motion.Reset();
    }

    private static int PrivilegeFromMask(byte privs) => privs switch
    {
        32 => 5,
        16 => 4,
        8 => 3,
        4 => 2,
        2 => 1,
        _ => 0,
    };
}
