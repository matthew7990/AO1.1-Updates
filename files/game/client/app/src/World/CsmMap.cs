using System;
using System.Buffers.Binary;
using System.IO;

namespace Argentum.Client.World;

public sealed class CsmMap
{
    public const int GridSize = 101;
    public const int PlayableSize = 100;
    public const int MinMapTile = 1;
    public const int MaxMapTile = 100;
    public const int TilePixels = 32;

    public int MapId { get; set; }
    public int ResourceId { get; set; }
    public string Name { get; private set; } = "";
    public MapAudioInfo Audio { get; private set; } = new();
    public Tile[,] Tiles { get; } = CreateTiles();

    private static Tile[,] CreateTiles()
    {
        var tiles = new Tile[GridSize, GridSize];
        for (var x = 0; x < GridSize; x++)
        {
            for (var y = 0; y < GridSize; y++)
            {
                tiles[x, y] = new Tile();
            }
        }
        return tiles;
    }

    public sealed class Tile
    {
        public byte Blocked;
        public int[] Graphics = new int[4];
        public short Trigger;
        public int ObjectIndex;
        public int ObjectAmount;
        public int ObjectElementalTags;
        /// <summary>True solo para ítems dropeados (ObjectCreate), no objetos fijos del mapa.</summary>
        public bool ObjectIsDroppedItem;
        public int NpcIndex;
        public int ExitMap;
        public int ExitX;
        public int ExitY;
    }

    public sealed class MapAudioInfo
    {
        public int MusicHigh;
        public int MusicLow;
        public string Ambient { get; set; } = "";
        public bool Rain;
        public bool Snow;
        public bool Fog;

        /// <summary>VB6 MapDat.ambient — "noche-día" separado por guión.</summary>
        public int ResolveAmbient(bool nightTime)
        {
            if (string.IsNullOrWhiteSpace(Ambient))
            {
                return 0;
            }
            var parts = Ambient.Split('-');
            if (parts.Length < 2)
            {
                return ParseInt(parts[0]);
            }
            return ParseInt(nightTime ? parts[0] : parts[1]);
        }

        private static int ParseInt(string? value) =>
            int.TryParse(value, out var parsed) ? parsed : 0;
    }

    public static CsmMap Load(string path)
    {
        var data = File.ReadAllBytes(path);
        var reader = new Reader(data);
        var map = new CsmMap();
        map.MapId = ExtractMapIdFromPath(path);
        var blocked = reader.ReadInt32();
        var layers = new int[4];
        for (var i = 0; i < 4; i++)
        {
            layers[i] = reader.ReadInt32();
        }
        var triggers = reader.ReadInt32();
        var lights = reader.ReadInt32();
        var particles = reader.ReadInt32();
        var npcs = reader.ReadInt32();
        var objects = reader.ReadInt32();
        var exits = reader.ReadInt32();
        _ = reader.ReadInt16();
        _ = reader.ReadInt16();
        _ = reader.ReadInt16();
        _ = reader.ReadInt16();
        map.Name = reader.ReadString16();
        _ = reader.ReadUInt8();
        _ = reader.ReadString16();
        map.Audio.MusicHigh = reader.ReadInt32();
        map.Audio.MusicLow = reader.ReadInt32();
        _ = reader.ReadUInt8();
        _ = reader.ReadString16();
        _ = reader.ReadString16();
        map.Audio.Ambient = reader.ReadString16();
        _ = reader.ReadInt32();
        _ = reader.ReadInt32();
        _ = reader.ReadInt32();
        _ = reader.ReadInt32();
        _ = reader.ReadString16();
        map.Audio.Rain = reader.ReadUInt8() != 0;
        map.Audio.Snow = reader.ReadUInt8() != 0;
        map.Audio.Fog = reader.ReadUInt8() != 0;

        ReadRecords(blocked, reader, map, (tile, r) => tile.Blocked = r.ReadUInt8());
        for (var layer = 0; layer < 4; layer++)
        {
            var layerIndex = layer;
            ReadRecords(layers[layer], reader, map, (tile, r) => tile.Graphics[layerIndex] = r.ReadInt32());
        }
        ReadRecords(triggers, reader, map, (tile, r) => tile.Trigger = r.ReadInt16());
        ReadRecords(particles, reader, map, (tile, r) => _ = r.ReadInt32());
        ReadRecords(lights, reader, map, (tile, r) =>
        {
            _ = r.ReadUInt32();
            _ = r.ReadUInt8();
        });
        ReadRecords(objects, reader, map, (tile, r) =>
        {
            tile.ObjectIndex = r.ReadInt16();
            tile.ObjectAmount = r.ReadInt16();
        });
        ReadRecords(npcs, reader, map, (tile, r) => tile.NpcIndex = r.ReadInt16());
        ReadRecords(exits, reader, map, (tile, r) =>
        {
            tile.ExitMap = r.ReadInt16();
            tile.ExitX = r.ReadInt16();
            tile.ExitY = r.ReadInt16();
        });
        return map;
    }

    private static int ExtractMapIdFromPath(string path)
    {
        var file = Path.GetFileNameWithoutExtension(path);
        if (file.StartsWith("mapa", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(file.AsSpan(4), out var mapId))
        {
            return mapId;
        }
        return 0;
    }

    private static void ReadRecords(int count, Reader reader, CsmMap map, Action<Tile, Reader> apply)
    {
        for (var i = 0; i < count; i++)
        {
            var x = reader.ReadInt16();
            var y = reader.ReadInt16();
            if (x < 1 || x > 100 || y < 1 || y > 100)
            {
                throw new InvalidDataException($"CSM coordinate out of range: {x},{y}");
            }
            apply(map.Tiles[x, y], reader);
        }
    }

    private sealed class Reader
    {
        private readonly byte[] _data;
        private int _offset;

        public Reader(byte[] data) => _data = data;

        public byte ReadUInt8()
        {
            Ensure(1);
            return _data[_offset++];
        }

        public short ReadInt16()
        {
            Ensure(2);
            var v = BinaryPrimitives.ReadInt16LittleEndian(_data.AsSpan(_offset, 2));
            _offset += 2;
            return v;
        }

        public int ReadInt32()
        {
            Ensure(4);
            var v = BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(_offset, 4));
            _offset += 4;
            return v;
        }

        public uint ReadUInt32()
        {
            Ensure(4);
            var v = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(_offset, 4));
            _offset += 4;
            return v;
        }

        public string ReadString16()
        {
            var length = ReadInt16();
            if (length < 0)
            {
                throw new InvalidDataException("Negative CSM string length");
            }
            Ensure(length);
            var value = System.Text.Encoding.Latin1.GetString(_data, _offset, length);
            _offset += length;
            return value;
        }

        private void Ensure(int size)
        {
            if (_offset + size > _data.Length)
            {
                throw new EndOfStreamException("Unexpected end of CSM file");
            }
        }
    }
}
