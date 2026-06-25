using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Argentum.Client.Resources;

public sealed class MoldDef
{
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public int[] DirCount = new int[5];
    public int TotalGrhs => DirCount[1] + DirCount[2] + DirCount[3] + DirCount[4] + 4;
}

public sealed class MoldCatalog
{
    private readonly MoldDef?[] _molds;

    public MoldCatalog(MoldDef?[] molds) => _molds = molds;

    public MoldDef? Get(int moldIndex)
    {
        if (moldIndex <= 0 || moldIndex >= _molds.Length)
        {
            return null;
        }
        return _molds[moldIndex];
    }

    public static MoldCatalog Load(string root)
    {
        var path = Path.Combine(root, "init", "moldes.ini");
        var count = 0;
        var sections = IniSections.Load(path);
        if (sections.TryGetValue("INIT", out var init))
        {
            count = ParseInt(init.GetValueOrDefault("Moldes"));
        }
        if (count <= 0)
        {
            throw new InvalidDataException($"Moldes missing in {path}");
        }
        var molds = new MoldDef?[count + 1];
        for (var i = 1; i <= count; i++)
        {
            if (!sections.TryGetValue($"Molde{i}", out var section))
            {
                continue;
            }
            var mold = new MoldDef
            {
                X = ParseInt(section.GetValueOrDefault("X")),
                Y = ParseInt(section.GetValueOrDefault("Y")),
                Width = ParseInt(section.GetValueOrDefault("Width")),
                Height = ParseInt(section.GetValueOrDefault("Height")),
            };
            for (var dir = 1; dir <= 4; dir++)
            {
                mold.DirCount[dir] = ParseInt(section.GetValueOrDefault($"Dir{dir}"));
            }
            molds[i] = mold;
        }
        return new MoldCatalog(molds);
    }

    private static int ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
}

internal static class IniSections
{
    public static Dictionary<string, Dictionary<string, string>> Load(string path)
    {
        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string>? current = null;
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';'))
            {
                continue;
            }
            if (line[0] == '[' && line[^1] == ']')
            {
                var section = line[1..^1];
                current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                sections[section] = current;
                continue;
            }
            var separator = line.IndexOf('=');
            if (separator <= 0 || current is null)
            {
                continue;
            }
            current[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }
        return sections;
    }
}

/// <summary>VB6 .dat values often include trailing comments: {@code 51672 ' arriba}.</summary>
internal static class IniValue
{
    public static int ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }
        var span = value.AsSpan().Trim();
        var end = span.Length;
        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];
            if (c is ' ' or '\t' or '\'')
            {
                end = i;
                break;
            }
        }
        return int.TryParse(span[..end].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }
}
