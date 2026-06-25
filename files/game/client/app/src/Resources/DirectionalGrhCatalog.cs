using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Argentum.Client.Resources;

/// <summary>VB6 WeaponAnimData / ShieldAnimData — grh por dirección (Dir1..Dir4).</summary>
public sealed class DirectionalGrhCatalog
{
    private readonly int[][] _entries;

    private DirectionalGrhCatalog(int[][] entries) => _entries = entries;

    public int GetGrh(int index, int heading)
    {
        if (index <= 0 || index >= _entries.Length || heading < 1 || heading > 4)
        {
            return 0;
        }
        return _entries[index][heading];
    }

    public static DirectionalGrhCatalog? TryLoad(string root, string fileName, string countKey, string sectionPrefix)
    {
        var path = Path.Combine(root, "init", fileName);
        if (!File.Exists(path))
        {
            return null;
        }
        var sections = ParseIni(path);
        if (!sections.TryGetValue("INIT", out var init) || !init.TryGetValue(countKey, out var countStr))
        {
            return null;
        }
        if (!int.TryParse(countStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 1)
        {
            return null;
        }
        var entries = new int[count + 1][];
        for (var i = 0; i <= count; i++)
        {
            entries[i] = new int[5];
        }
        for (var i = 1; i <= count; i++)
        {
            var key = sectionPrefix + i;
            if (!sections.TryGetValue(key, out var sec))
            {
                continue;
            }
            if (sec.TryGetValue("Std", out var stdStr) && int.TryParse(stdStr, out var std) && std > 0)
            {
                continue;
            }
            for (var dir = 1; dir <= 4; dir++)
            {
                if (sec.TryGetValue("Dir" + dir, out var grhStr) &&
                    int.TryParse(grhStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var grh))
                {
                    entries[i][dir] = grh;
                }
            }
        }
        return new DirectionalGrhCatalog(entries);
    }

    private static Dictionary<string, Dictionary<string, string>> ParseIni(string path)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var section = "";
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';'))
            {
                continue;
            }
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1];
                if (!result.ContainsKey(section))
                {
                    result[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                continue;
            }
            var eq = line.IndexOf('=');
            if (eq <= 0 || section.Length == 0)
            {
                continue;
            }
            if (!result.TryGetValue(section, out var sec))
            {
                sec = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                result[section] = sec;
            }
            sec[line[..eq].Trim()] = line[(eq + 1)..].Trim();
        }
        return result;
    }
}
