using System;
using System.Globalization;
using System.IO;

namespace Argentum.Client.Resources;

public sealed class GrhDef
{
    public int FileNum;
    public short SX;
    public short SY;
    public short PixelWidth;
    public short PixelHeight;
    public float TileWidth;
    public float TileHeight;
    public int[] Frames = [];
    public float Speed;
    public bool Animated => Frames.Length > 1;

    public void SyncTileFootprint()
    {
        TileWidth = PixelWidth / (float)World.CsmMap.TilePixels;
        TileHeight = PixelHeight / (float)World.CsmMap.TilePixels;
    }
}

public sealed class GrhCatalog
{
    private readonly GrhDef?[] _defs;

    public GrhCatalog(GrhDef?[] defs) => _defs = defs;

    public int Capacity => _defs.Length - 1;

    public GrhDef? Get(int index)
    {
        if (index <= 0 || index >= _defs.Length)
        {
            return null;
        }
        return _defs[index];
    }

    public float GetFrameDurationMs(int grhIndex)
    {
        var def = Get(grhIndex);
        if (def is null || !def.Animated)
        {
            return 0f;
        }
        var animSpeed = Math.Max(def.Speed, 0.0001f);
        return 1f / animSpeed / World.CharacterMotion.EngineBaseSpeed;
    }

    public int GetAnimationDurationMs(int grhIndex)
    {
        var def = Get(grhIndex);
        if (def is null || !def.Animated || def.Frames.Length <= 0)
        {
            return 0;
        }
        return (int)MathF.Ceiling(GetFrameDurationMs(grhIndex) * def.Frames.Length);
    }

    public int ResolveFrame(int grhIndex, int tick)
    {
        var def = Get(grhIndex);
        if (def is null)
        {
            return grhIndex;
        }
        if (!def.Animated)
        {
            return grhIndex;
        }
        var frameDurationMs = Math.Max(GetFrameDurationMs(grhIndex), 1f);
        var frame = (int)(tick / frameDurationMs) % def.Frames.Length;
        return def.Frames[frame];
    }

    /// <summary>Grh listo para dibujar (resuelve animación y copia dimensiones del frame).</summary>
    public GrhDef? ResolveDrawable(int grhIndex, int tick = 0)
    {
        var frameIndex = ResolveFrame(grhIndex, tick);
        var def = Get(frameIndex);
        if (def is null)
        {
            return null;
        }
        if (def.FileNum > 0 && def.PixelWidth > 0)
        {
            return def;
        }
        if (def.Animated && def.Frames.Length > 0 && def.Frames[0] != frameIndex)
        {
            return ResolveDrawable(def.Frames[0], tick);
        }
        return def.FileNum > 0 ? def : null;
    }

    public static GrhCatalog Load(string root)
    {
        var iniPath = Path.Combine(root, "init", "Graficos.ini");
        if (File.Exists(iniPath))
        {
            return LoadIni(iniPath);
        }
        return LoadInd(Path.Combine(root, "init", "graficos.ind"));
    }

    // VB6 Recursos.bas LoadGrhIni — fuente principal de gráficos en AO 20.
    private static GrhCatalog LoadIni(string path)
    {
        var maxGrh = 0;
        var inGraphics = false;
        GrhDef?[]? defs = null;

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }
            if (defs is null)
            {
                if (line.StartsWith("NumGrh=", StringComparison.OrdinalIgnoreCase))
                {
                    maxGrh = ParseInt(line.AsSpan("NumGrh=".Length));
                    if (maxGrh <= 0)
                    {
                        throw new InvalidDataException($"NumGrh inválido en {path}");
                    }
                    defs = new GrhDef?[maxGrh + 1];
                }
                continue;
            }
            if (!inGraphics)
            {
                if (line.Equals("[GRAPHICS]", StringComparison.OrdinalIgnoreCase))
                {
                    inGraphics = true;
                }
                continue;
            }
            if (!line.StartsWith("Grh", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var eq = line.IndexOf('=');
            if (eq <= 3)
            {
                continue;
            }
            var grh = ParseInt(line.AsSpan(3, eq - 3));
            if (grh <= 0 || grh > maxGrh)
            {
                continue;
            }
            var fields = line[(eq + 1)..].Split('-');
            if (fields.Length < 2)
            {
                continue;
            }
            var numFrames = ParseInt(fields[0]);
            var def = new GrhDef();
            if (numFrames > 1)
            {
                if (fields.Length < numFrames + 2)
                {
                    continue;
                }
                def.Frames = new int[numFrames];
                for (var i = 0; i < numFrames; i++)
                {
                    def.Frames[i] = ParseInt(fields[i + 1]);
                }
                def.Speed = ParseFloat(fields[numFrames + 1]);
                CopyDrawableDims(def, defs[def.Frames[0]]);
            }
            else if (numFrames == 1)
            {
                if (fields.Length < 6)
                {
                    continue;
                }
                def.FileNum = ParseInt(fields[1]);
                def.SX = (short)ParseInt(fields[2]);
                def.SY = (short)ParseInt(fields[3]);
                def.PixelWidth = (short)ParseInt(fields[4]);
                def.PixelHeight = (short)ParseInt(fields[5]);
                def.SyncTileFootprint();
                def.Frames = [grh];
            }
            else
            {
                continue;
            }
            defs[grh] = def;
        }

        if (defs is null)
        {
            throw new InvalidDataException($"NumGrh no encontrado en {path}");
        }

        for (var i = 1; i <= maxGrh; i++)
        {
            var def = defs[i];
            if (def is null || !def.Animated || def.PixelWidth > 0)
            {
                continue;
            }
            CopyDrawableDims(def, defs[def.Frames[0]]);
        }
        return new GrhCatalog(defs);
    }

    private static GrhCatalog LoadInd(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        _ = reader.ReadInt32(); // fileVersion
        var count = reader.ReadInt32();
        var defs = new GrhDef?[count + 1];
        while (stream.Position < stream.Length)
        {
            var grh = reader.ReadInt32();
            if (grh <= 0 || grh > count)
            {
                break;
            }
            var def = new GrhDef();
            var numFrames = reader.ReadInt16();
            if (numFrames > 1)
            {
                def.Frames = new int[numFrames];
                for (var i = 0; i < numFrames; i++)
                {
                    def.Frames[i] = reader.ReadInt32();
                }
                def.Speed = reader.ReadSingle();
                defs[grh] = def;
            }
            else
            {
                def.FileNum = reader.ReadInt32();
                def.SX = reader.ReadInt16();
                def.SY = reader.ReadInt16();
                def.PixelWidth = reader.ReadInt16();
                def.PixelHeight = reader.ReadInt16();
                def.SyncTileFootprint();
                def.Frames = [grh];
                defs[grh] = def;
            }
            if (grh == count)
            {
                break;
            }
        }
        for (var i = 1; i <= count; i++)
        {
            var def = defs[i];
            if (def is null || !def.Animated || def.PixelWidth > 0)
            {
                continue;
            }
            CopyDrawableDims(def, defs[def.Frames[0]]);
        }
        return new GrhCatalog(defs);
    }

    private static void CopyDrawableDims(GrhDef target, GrhDef? source)
    {
        if (source is null || source.PixelWidth <= 0)
        {
            return;
        }
        target.FileNum = source.FileNum;
        target.SX = source.SX;
        target.SY = source.SY;
        target.PixelWidth = source.PixelWidth;
        target.PixelHeight = source.PixelHeight;
        target.TileWidth = source.TileWidth;
        target.TileHeight = source.TileHeight;
    }

    private static int ParseInt(ReadOnlySpan<char> text) =>
        int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static float ParseFloat(string text) =>
        float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
}
