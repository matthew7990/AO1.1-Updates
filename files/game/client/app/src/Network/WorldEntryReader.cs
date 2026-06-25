using System;
using System.IO;
using System.Threading.Tasks;
using Argentum.Client.Resources;
using Argentum.Client.Network;
using Argentum.Client.Protocol;
using Godot;

namespace Argentum.Client.World;

public sealed class WorldEntryReader
{
    public async Task<WorldSession> ReadAsync(IAsyncFrameReader connection, byte[]? firstFrame = null)
    {
        var session = new WorldSession();
        if (firstFrame is not null)
        {
            ApplyFrame(firstFrame, session);
        }
        while (!session.LoggedIn)
        {
            var payload = await connection.ReadFrameAsync();
            ApplyFrame(payload, session);
        }
        return session;
    }

    private static void ApplyFrame(byte[] payload, WorldSession session)
    {
        var reader = new LegacyPacketReader(payload);
        var packet = (ServerPacketId)reader.ReadInt16();
        try
        {
            ApplyPacketBody(packet, ref reader, session);
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException(
                $"Incomplete packet {packet} (payload={payload.Length} bytes, consumed={payload.Length - reader.Remaining})",
                exception);
        }
        if (reader.Remaining != 0)
        {
            GD.PushWarning($"Packet {packet} left {reader.Remaining} unread bytes");
        }
    }

    private static void ApplyPacketBody(ServerPacketId packet, ref LegacyPacketReader reader, WorldSession session)
    {
            switch (packet)
            {
                case ServerPacketId.SendClientToggles:
                    var toggleCount = reader.ReadInt16();
                    for (var i = 0; i < toggleCount; i++)
                    {
                        _ = reader.ReadString8();
                    }
                    break;
                case ServerPacketId.Intervals:
                    session.AttackIntervalMs = Math.Max(1, reader.ReadInt32());
                    _ = reader.ReadInt32(); // Bow
                    session.MagicIntervalMs = Math.Max(1, reader.ReadInt32());
                    _ = reader.ReadInt32(); // ExtractWork
                    _ = reader.ReadInt32(); // BuildWork
                    session.WalkIntervalMs = Math.Max(1, reader.ReadInt32());
                    for (var i = 0; i < 10; i++)
                    {
                        _ = reader.ReadInt32();
                    }
                    break;
                case ServerPacketId.UserIndexInServer:
                    session.UserIndex = reader.ReadInt16();
                    break;
                case ServerPacketId.Hora:
                    _ = reader.ReadInt32();
                    _ = reader.ReadInt32();
                    break;
                case ServerPacketId.ChangeMap:
                    session.MapId = reader.ReadInt16();
                    session.MapResource = reader.ReadInt16();
                    break;
                case ServerPacketId.AreaChanged:
                    // VB6 CambioDeArea no actualiza UserPos en login.
                    _ = reader.ReadUInt8();
                    _ = reader.ReadUInt8();
                    break;
                case ServerPacketId.BlockPosition:
                    BlockPositionReader.Apply(ref reader, session);
                    break;
                case ServerPacketId.CharacterCreate:
                    ParseCharacterCreate(ref reader, session);
                    break;
                case ServerPacketId.UserCharIndexInServer:
                    session.CharIndex = reader.ReadInt16();
                    break;
                case ServerPacketId.UpdateUserStats:
                    UserStatsReader.ReadInto(ref reader, session);
                    break;
                case ServerPacketId.UpdateHungerAndThirst:
                    _ = reader.ReadUInt8();
                    _ = reader.ReadUInt8();
                    _ = reader.ReadUInt8();
                    _ = reader.ReadUInt8();
                    break;
                case ServerPacketId.ChangeInventorySlot:
                    InventorySlotReader.Apply(ref reader, session.Inventory);
                    break;
                case ServerPacketId.ChangeSpellSlot:
                    ChangeSpellSlotReader.Apply(ref reader, session.Spells);
                    break;
                case ServerPacketId.InventoryUnlockSlots:
                    var tier = reader.ReadUInt8();
                    session.Inventory.UnlockedSlots = PlayerInventory.DefaultUnlocked + tier * 6;
                    if (session.Inventory.UnlockedSlots > PlayerInventory.MaxSlots)
                    {
                        session.Inventory.UnlockedSlots = PlayerInventory.MaxSlots;
                    }
                    break;
                case ServerPacketId.Logged:
                    _ = reader.ReadBoolean();
                    session.LoggedIn = true;
                    break;
                default:
                    throw new InvalidDataException($"Unexpected world-entry packet: {packet}");
            }
    }

    private static void ParseCharacterCreate(ref LegacyPacketReader reader, WorldSession session)
    {
        session.CharIndex = reader.ReadInt16();
        session.Body = reader.ReadInt16();
        session.Head = reader.ReadInt16();
        session.Heading = reader.ReadUInt8();
        session.TileX = reader.ReadUInt8();
        session.TileY = reader.ReadUInt8();
        session.GfxWeapon = reader.ReadInt16();
        session.GfxShield = reader.ReadInt16();
        session.GfxHelmet = reader.ReadInt16();
        _ = reader.ReadInt16(); // cart
        _ = reader.ReadInt16(); // backpack
        _ = reader.ReadInt16(); // fx
        _ = reader.ReadInt16(); // fx loops
        session.CharacterName = reader.ReadString8();
        _ = reader.ReadUInt8();
        session.Privilege = PrivilegeFromMask(reader.ReadUInt8());
        _ = reader.ReadUInt8();
        for (var i = 0; i < 7; i++)
        {
            _ = reader.ReadString8();
        }
        _ = reader.ReadReal32();
        _ = reader.ReadUInt8();
        _ = reader.ReadUInt8();
        _ = reader.ReadInt16();
        _ = reader.ReadInt16();
        _ = reader.ReadUInt8();
        session.MinHp = reader.ReadInt32();
        session.MaxHp = reader.ReadInt32();
        session.MinMana = reader.ReadInt32();
        session.MaxMana = reader.ReadInt32();
        _ = reader.ReadUInt8();
        _ = reader.ReadUInt8();
        _ = reader.ReadUInt8();
        _ = reader.ReadUInt8();
        _ = reader.ReadUInt8();
        _ = reader.ReadInt16();
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

    private static CsmMap? TryLoadMap(int mapId) => MapLoader.TryLoad(mapId);
}
