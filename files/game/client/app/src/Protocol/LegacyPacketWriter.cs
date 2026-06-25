using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Argentum.Client.Protocol;

public sealed class LegacyPacketWriter
{
    private readonly MemoryStream _stream = new();

    public void WriteInt8(sbyte value) => WriteUInt8((byte)value);

    public void WriteUInt8(byte value) => _stream.WriteByte(value);

    public void WriteInt16(short value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteInt16LittleEndian(bytes, value);
        _stream.Write(bytes);
    }

    public void WriteInt32(int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        _stream.Write(bytes);
    }

    public void WriteVarInt(uint value)
    {
        var v = value;
        while (v > 0x7F)
        {
            WriteUInt8((byte)((v & 0x7F) | 0x80));
            v >>= 7;
        }
        WriteUInt8((byte)(v & 0x7F));
    }

    public void WriteReal32(float value) => WriteInt32(BitConverter.SingleToInt32Bits(value));

    public void WriteBoolean(bool value) => WriteUInt8(value ? (byte)1 : (byte)0);

    public void WriteString8(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteVarInt((uint)bytes.Length);
        _stream.Write(bytes);
    }

    public byte[] ToArray() => _stream.ToArray();

    public void Clear() => _stream.SetLength(0);
}
