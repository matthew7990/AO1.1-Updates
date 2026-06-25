using System;
using System.Collections.Generic;
using System.IO;

namespace Argentum.Client.Resources;

/// <summary>Subset de HechizoData / Hechizos.dat para UI y tooltips.</summary>
public sealed class SpellCatalog
{
    public sealed class Entry
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public string Desc { get; init; } = "";
        public int ManaCost { get; init; }
        public int StaCost { get; init; }
        public int MinSkill { get; init; }
        public int IconoIndex { get; init; }
        public bool IsBindable { get; init; }
        public int Cooldown { get; init; }
    }

    private readonly Dictionary<int, Entry> _byId = new();

    public static SpellCatalog? TryLoad(string resourcesRoot)
    {
        var path = Path.Combine(resourcesRoot, "Dat", "Hechizos.dat");
        if (!File.Exists(path))
        {
            return null;
        }
        var catalog = new SpellCatalog();
        catalog.Load(path);
        return catalog;
    }

    private void Load(string path)
    {
        string? section = null;
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] is '\'' or ';')
            {
                continue;
            }
            if (line[0] == '[')
            {
                FlushSection(section, fields);
                section = line.Trim('[', ']');
                fields.Clear();
                continue;
            }
            var eq = line.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }
            fields[line[..eq].Trim()] = line[(eq + 1)..].Trim();
        }
        FlushSection(section, fields);
    }

    private void FlushSection(string? section, Dictionary<string, string> fields)
    {
        if (section is null || !section.StartsWith("HECHIZO", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        if (!int.TryParse(section.AsSpan(7), out var id) || id <= 0)
        {
            return;
        }
        _byId[id] = new Entry
        {
            Id = id,
            Name = Val(fields, "Nombre"),
            Desc = Val(fields, "Desc"),
            ManaCost = ParseInt(Val(fields, "ManaRequerido")),
            StaCost = ParseInt(Val(fields, "StaRequerido")),
            MinSkill = ParseInt(Val(fields, "MinSkill")),
            IconoIndex = ParseInt(Val(fields, "IconoIndex")),
            IsBindable = ParseInt(Val(fields, "IsBindable")) > 0,
            Cooldown = ParseInt(Val(fields, "CoolDown")),
        };
    }

    public Entry? Get(int id) => _byId.TryGetValue(id, out var e) ? e : null;

    private static string Val(Dictionary<string, string> fields, string key) =>
        fields.TryGetValue(key, out var v) ? v : "";

    private static int ParseInt(string value) =>
        int.TryParse(value, out var n) ? n : 0;
}
