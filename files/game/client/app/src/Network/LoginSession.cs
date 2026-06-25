using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Argentum.Client.Models;
using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

public sealed class LoginSession : IAsyncDisposable
{
    private FrameConnection? _connection;

    public bool IsConnected => _connection is not null;

    public async Task ConnectAsync(string host, int port)
    {
        _connection = new FrameConnection();
        await _connection.ConnectAsync(host, port);
        ValidateConnected(await _connection.ReadFrameAsync());
    }

    public async Task<IReadOnlyList<CharacterSummary>> CreateAccountAsync(string email, string password) =>
        await AuthenticateAsync(ClientPacketId.CreateAccount, email, password);

    public async Task<IReadOnlyList<CharacterSummary>> LoginAccountAsync(string email, string password) =>
        await AuthenticateAsync(ClientPacketId.LoginAccount, email, password);

    public async Task EnterExistingCharacterAsync(CharacterSummary character)
    {
        var writer = new LegacyPacketWriter();
        writer.WriteInt16((short)ClientPacketId.LoginExistingChar);
        writer.WriteInt32(character.Id);
        writer.WriteString8(character.Name);
        await _connection!.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendNewCharacterAsync(
        string name, int race, int gender, int classId, int head, int home)
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("No hay conexión activa con el servidor.");
        }
        var writer = new LegacyPacketWriter();
        writer.WriteInt16((short)ClientPacketId.LoginNewChar);
        writer.WriteString8(name);
        writer.WriteVarInt((uint)race);
        writer.WriteVarInt((uint)gender);
        writer.WriteVarInt((uint)classId);
        writer.WriteVarInt((uint)head);
        writer.WriteVarInt((uint)home);
        var payload = writer.ToArray();
        NetDiagnostics.Log("TX", $"LoginNewChar name={name} race={race} gender={gender} class={classId} head={head} home={home} bytes={payload.Length}");
        await _connection.WriteFrameAsync(payload);
    }

    public FrameConnection RequireConnection()
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("No hay conexión activa.");
        }
        return _connection;
    }

    public FrameConnection TakeConnection()
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("No hay conexión activa.");
        }
        var connection = _connection;
        _connection = null;
        return connection;
    }

    private async Task<IReadOnlyList<CharacterSummary>> AuthenticateAsync(
        ClientPacketId packet, string email, string password)
    {
        var writer = new LegacyPacketWriter();
        writer.WriteInt16((short)packet);
        writer.WriteString8(email);
        writer.WriteString8(password);
        await _connection!.WriteFrameAsync(writer.ToArray());
        var response = await _connection.ReadFrameAsync();
        if (TryParseError(response, out var message))
        {
            throw new InvalidOperationException(message);
        }
        return AccountCharacterListReader.Parse(response);
    }

    private async Task WriteAndCheckErrorAsync(byte[] payload)
    {
        await _connection!.WriteFrameAsync(payload);
        var response = await _connection.ReadFrameAsync();
        if (TryParseError(response, out var message))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void ValidateConnected(byte[] payload)
    {
        var reader = new LegacyPacketReader(payload);
        if ((ServerPacketId)reader.ReadInt16() != ServerPacketId.Connected)
        {
            throw new InvalidOperationException("Handshake inválido con el servidor.");
        }
        reader.SkipSafeArrayInt8();
    }

    public static bool TryParseError(byte[] payload, out string message)
    {
        message = string.Empty;
        if (payload.Length < 2)
        {
            return false;
        }
        var reader = new LegacyPacketReader(payload);
        if ((ServerPacketId)reader.ReadInt16() != ServerPacketId.ErrorMsg)
        {
            return false;
        }
        message = reader.ReadString8();
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
