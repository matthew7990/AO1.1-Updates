using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Argentum.Client.Network;

/// <summary>Aurora.Network framing: uint16 LE length + payload (igual que ao-server-go).</summary>
public sealed class FrameConnection : IAsyncFrameReader, IAsyncDisposable
{
    public const int DefaultMaxPacket = 1024 * 1024;
    private readonly TcpClient _client = new();
    private NetworkStream? _stream;

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await _client.ConnectAsync(host, port, cancellationToken);
        _client.NoDelay = true;
        _stream = _client.GetStream();
    }

    public async Task<byte[]> ReadFrameAsync(CancellationToken cancellationToken = default)
    {
        var stream = RequireStream();
        var header = new byte[2];
        await stream.ReadExactlyAsync(header, cancellationToken);
        var size = BinaryPrimitives.ReadUInt16LittleEndian(header);
        if (size == 0 || size > ushort.MaxValue)
        {
            throw new InvalidDataException($"Invalid AO frame size: {size}");
        }
        var payload = new byte[size];
        await stream.ReadExactlyAsync(payload, cancellationToken);
        return payload;
    }

    public async Task WriteFrameAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        if (payload.IsEmpty || payload.Length > DefaultMaxPacket)
        {
            throw new ArgumentOutOfRangeException(nameof(payload));
        }
        var header = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(header, (ushort)payload.Length);
        var stream = RequireStream();
        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private NetworkStream RequireStream() =>
        _stream ?? throw new InvalidOperationException("The AO connection is not open");

    public async ValueTask DisposeAsync()
    {
        if (_stream is not null)
        {
            await _stream.DisposeAsync();
        }
        _client.Dispose();
    }
}
