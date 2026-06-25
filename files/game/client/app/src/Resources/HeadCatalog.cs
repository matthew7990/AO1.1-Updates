using System.IO;
using Godot;

namespace Argentum.Client.Resources;

public sealed class HeadCatalog
{
    private readonly int[][] _heads;

    public HeadCatalog(int[][] heads) => _heads = heads;

    public int GetGrh(int headIndex, int heading)
    {
        if (headIndex <= 0 || headIndex >= _heads.Length || heading < 1 || heading > 4)
        {
            return 0;
        }
        return _heads[headIndex][heading];
    }

    /// <summary>
    /// Muchas cabezas AO solo tienen arte en N/E o S/W; si el grh del .ind apunta a fila vacía,
    /// usar la dirección opuesta (mismo comportamiento visual que el cliente con alpha correcto).
    /// </summary>
    public void ResolveDirectionFallbacks(GrhCatalog grhs, TextureCache textures)
    {
        int[] opposite = [0, 3, 4, 1, 2];
        for (var i = 1; i < _heads.Length; i++)
        {
            if (_heads[i] is null)
            {
                continue;
            }
            for (var h = 1; h <= 4; h++)
            {
                var grh = _heads[i][h];
                if (grh <= 0 || GrhHasOpaquePixels(grhs, textures, grh))
                {
                    continue;
                }
                var fallback = _heads[i][opposite[h]];
                if (fallback > 0 && GrhHasOpaquePixels(grhs, textures, fallback))
                {
                    Godot.GD.Print($"[HeadCatalog] head={i} hdg={h} grh={grh} vacío → fallback hdg={opposite[h]} grh={fallback}");
                    _heads[i][h] = fallback;
                }
            }
        }
    }

    private static bool GrhHasOpaquePixels(GrhCatalog grhs, TextureCache textures, int grhIndex)
    {
        var def = grhs.Get(grhIndex);
        if (def is null || def.FileNum <= 0)
        {
            return false;
        }
        var texture = textures.Get(def.FileNum);
        if (texture is null)
        {
            return false;
        }
        var image = texture.GetImage();
        if (image is null)
        {
            return false;
        }
        for (var y = def.SY; y < def.SY + def.PixelHeight; y++)
        {
            for (var x = def.SX; x < def.SX + def.PixelWidth; x++)
            {
                if (image.GetPixel(x, y).A > 10)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static HeadCatalog Load(string root)
    {
        return Load(root, "cabezas.ind");
    }

    public static HeadCatalog Load(string root, string fileName)
    {
        var path = Path.Combine(root, "init", fileName);
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        // MiCabecera (255-byte text) + DIU metadata; Numheads is int16 at offset 263.
        stream.Seek(263, SeekOrigin.Begin);
        var count = reader.ReadInt16();
        var heads = new int[count + 1][];
        for (var i = 1; i <= count; i++)
        {
            heads[i] = new int[5];
            for (var h = 1; h <= 4; h++)
            {
                heads[i][h] = reader.ReadInt32();
            }
        }
        return new HeadCatalog(heads);
    }

    public void LogSample(int headIndex)
    {
        if (headIndex <= 0 || headIndex >= _heads.Length)
        {
            return;
        }
        Godot.GD.Print(
            $"[HeadCatalog] head={headIndex} grhs N={_heads[headIndex][1]} E={_heads[headIndex][2]} " +
            $"S={_heads[headIndex][3]} W={_heads[headIndex][4]}");
    }
}
