using System.Collections.Generic;
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
    /// Muchas cabezas AO solo tienen arte en N/E o S/W; si el grh del .ind apunta a fila vacia,
    /// usar la direccion opuesta.
    /// </summary>
    public void ResolveDirectionFallbacks(GrhCatalog grhs, TextureCache textures)
    {
        int[] opposite = [0, 3, 4, 1, 2];
        var opaqueByGrh = new Dictionary<int, bool>();

        for (var i = 1; i < _heads.Length; i++)
        {
            var head = _heads[i];
            if (head is null)
            {
                continue;
            }

            for (var h = 1; h <= 4; h++)
            {
                var grh = head[h];
                if (grh <= 0 || GrhHasOpaquePixels(grhs, textures, opaqueByGrh, grh))
                {
                    continue;
                }

                var fallback = head[opposite[h]];
                if (fallback > 0 && GrhHasOpaquePixels(grhs, textures, opaqueByGrh, fallback))
                {
                    head[h] = fallback;
                }
            }
        }
    }

    private static bool GrhHasOpaquePixels(
        GrhCatalog grhs,
        TextureCache textures,
        Dictionary<int, bool> opaqueByGrh,
        int grhIndex)
    {
        if (opaqueByGrh.TryGetValue(grhIndex, out var cached))
        {
            return cached;
        }

        var def = grhs.Get(grhIndex);
        if (def is null || def.FileNum <= 0)
        {
            opaqueByGrh[grhIndex] = false;
            return false;
        }

        var image = textures.GetImage(def.FileNum);
        if (image is null)
        {
            opaqueByGrh[grhIndex] = false;
            return false;
        }

        if (HasOpaqueSample(image, def.SX, def.SY, def.PixelWidth, def.PixelHeight))
        {
            opaqueByGrh[grhIndex] = true;
            return true;
        }

        var endY = def.SY + def.PixelHeight;
        var endX = def.SX + def.PixelWidth;
        for (var y = def.SY; y < endY; y++)
        {
            for (var x = def.SX; x < endX; x++)
            {
                if (image.GetPixel(x, y).A > 10)
                {
                    opaqueByGrh[grhIndex] = true;
                    return true;
                }
            }
        }

        opaqueByGrh[grhIndex] = false;
        return false;
    }

    private static bool HasOpaqueSample(Image image, int sx, int sy, int width, int height)
    {
        const int sampleGrid = 4;
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        var stepX = System.Math.Max(1, width / sampleGrid);
        var stepY = System.Math.Max(1, height / sampleGrid);
        var endX = sx + width;
        var endY = sy + height;

        for (var y = sy; y < endY; y += stepY)
        {
            for (var x = sx; x < endX; x += stepX)
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

        GD.Print(
            $"[HeadCatalog] head={headIndex} grhs N={_heads[headIndex][1]} E={_heads[headIndex][2]} " +
            $"S={_heads[headIndex][3]} W={_heads[headIndex][4]}");
    }
}
