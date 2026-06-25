using System;
using System.Buffers.Binary;
using System.IO;

namespace Argentum.Client.Protocol;

public ref struct LegacyPacketReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _offset;

    public LegacyPacketReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _offset = 0;
    }

    public int Remaining => _data.Length - _offset;

    public byte ReadUInt8() => Read(1)[0];

    public short ReadInt16() => BinaryPrimitives.ReadInt16LittleEndian(Read(2));

    public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(Read(4));

    public uint ReadVarInt()
    {
        uint result = 0;
        var shift = 0;
        for (var i = 0; i < 10; i++)
        {
            var b = ReadUInt8();
            result |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return result;
            }
            shift += 7;
        }
        throw new InvalidDataException("VarInt demasiado largo");
    }

    public float ReadReal32() => BitConverter.Int32BitsToSingle(ReadInt32());

    public bool ReadBoolean() => ReadUInt8() != 0;

    public string ReadString8()
    {
        var length = (int)ReadVarInt();
        if (length == 0)
        {
            return string.Empty;
        }
        return System.Text.Encoding.UTF8.GetString(Read(length));
    }

    public void SkipSafeArrayInt8()
    {
        if (Remaining <= 0)
        {
            return;
        }
        var dims = ReadVarInt();
        var total = 0;
        for (uint i = 0; i < dims; i++)
        {
            _ = ReadVarInt(); // lower
            total += (int)ReadVarInt(); // count
        }
        _ = Read(total);
    }

    private ReadOnlySpan<byte> Read(int size)
    {
        if (size < 0 || Remaining < size)
        {
            throw new EndOfStreamException("Incomplete Argentum packet");
        }
        var value = _data.Slice(_offset, size);
        _offset += size;
        return value;
    }
}
