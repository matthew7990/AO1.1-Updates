using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace Argentum.Client.Resources;

/// <summary>VB6 FxData desde init/fxs.ind (tIndiceFx).</summary>
public sealed class FxCatalog
{
    public sealed class FxDef
    {
        public int Animacion { get; init; }
        public short OffsetX { get; init; }
        public short OffsetY { get; init; }
        public int IsPng { get; init; }
    }

    private readonly List<FxDef> _fx = new() { new FxDef() };

    public static FxCatalog? TryLoad(string resourcesRoot)
    {
        var path = Path.Combine(resourcesRoot, "init", "fxs.ind");
        if (!File.Exists(path))
        {
            return null;
        }
        var catalog = new FxCatalog();
        catalog.Load(path);
        return catalog;
    }

    private void Load(string path)
    {
        var data = File.ReadAllBytes(path);
        if (data.Length < 22)
        {
            return;
        }
        // Skip MiCabecera (22 bytes in VB6 binary client resources).
        var offset = 22;
        var count = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, 4));
        offset += 4;
        for (var i = 0; i < count; i++)
        {
            if (offset + 12 > data.Length)
            {
                break;
            }
            var anim = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, 4));
            offset += 4;
            var ox = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(offset, 2));
            offset += 2;
            var oy = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(offset, 2));
            offset += 2;
            var isPng = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, 4));
            offset += 4;
            _fx.Add(new FxDef { Animacion = anim, OffsetX = ox, OffsetY = oy, IsPng = isPng });
        }
    }

    public FxDef? Get(int index)
    {
        if (index <= 0 || index >= _fx.Count)
        {
            return null;
        }
        return _fx[index];
    }
}
