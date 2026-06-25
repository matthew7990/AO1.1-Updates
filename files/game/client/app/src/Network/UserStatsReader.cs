using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

/// <summary>VB6 Protocol.HandleUpdateUserStats — orden de campos idéntico.</summary>
public static class UserStatsReader
{
    public static void ReadInto(ref LegacyPacketReader reader, WorldSession session)
    {
        session.MaxHp = reader.ReadInt16();
        session.MinHp = reader.ReadInt16();
        session.Shield = reader.ReadInt32();
        session.MaxMana = reader.ReadInt16();
        session.MinMana = reader.ReadInt16();
        session.MaxSta = reader.ReadInt16();
        session.MinSta = reader.ReadInt16();
        session.Gold = reader.ReadInt32();
        session.GoldPerLevel = reader.ReadInt32();
        session.Level = reader.ReadUInt8();
        session.ExpNext = reader.ReadInt32();
        session.Exp = reader.ReadInt32();
        session.ClassId = reader.ReadUInt8();
        session.IsDead = session.MinHp <= 0;
    }
}
