using System;
using System.Collections.Generic;
using Argentum.Client.Models;
using Argentum.Client.Protocol;

namespace Argentum.Client.Network;

public static class AccountCharacterListReader
{
    public static IReadOnlyList<CharacterSummary> Parse(byte[] payload)
    {
        var reader = new LegacyPacketReader(payload);
        if ((ServerPacketId)reader.ReadInt16() != ServerPacketId.AccountCharacterList)
        {
            throw new InvalidOperationException("Paquete inesperado (se esperaba lista de personajes).");
        }
        var count = (int)reader.ReadVarInt();
        var list = new List<CharacterSummary>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(new CharacterSummary
            {
                Id = (int)reader.ReadVarInt(),
                Name = reader.ReadString8(),
                Body = (int)reader.ReadVarInt(),
                Head = (int)reader.ReadVarInt(),
                Class = (int)reader.ReadVarInt(),
                Map = (int)reader.ReadVarInt(),
                PosX = (int)reader.ReadVarInt(),
                PosY = (int)reader.ReadVarInt(),
                Level = (int)reader.ReadVarInt(),
                Status = (int)reader.ReadVarInt(),
                Helmet = (int)reader.ReadVarInt(),
                Shield = (int)reader.ReadVarInt(),
                Weapon = (int)reader.ReadVarInt(),
                Backpack = (int)reader.ReadVarInt(),
            });
        }
        return list;
    }
}
