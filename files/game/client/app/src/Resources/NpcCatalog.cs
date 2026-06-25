using System;
using System.Collections.Generic;
using System.IO;

namespace Argentum.Client.Resources;

/// <summary>Apariencia estática de NPCs desde npcs.dat (spawns del CSM).</summary>
public sealed class NpcCatalog
{
    private readonly Dictionary<int, NpcDef> _npcs = new();

    public NpcDef? Get(int npcNumber) =>
        _npcs.TryGetValue(npcNumber, out var def) ? def : null;

    public static NpcCatalog? TryLoad(string root)
    {
        var path = Path.Combine(root, "Dat", "npcs.dat");
        if (!File.Exists(path))
        {
            Godot.GD.PushWarning($"NpcCatalog: missing {path}");
            return null;
        }

        var sections = IniSections.Load(path);
        var catalog = new NpcCatalog();
        foreach (var (key, section) in sections)
        {
            if (key.Length <= 3 || !key.StartsWith("NPC", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (!int.TryParse(key.AsSpan(3), out var num) || num <= 0)
            {
                continue;
            }

            var body = IniValue.ParseInt(section.GetValueOrDefault("Body"));
            if (body <= 0)
            {
                continue;
            }

            var heading = IniValue.ParseInt(section.GetValueOrDefault("Heading"));
            if (heading is < 1 or > 4)
            {
                heading = 3;
            }

            catalog._npcs[num] = new NpcDef
            {
                Body = body,
                Head = IniValue.ParseInt(section.GetValueOrDefault("Head")),
                Heading = heading,
                Weapon = IniValue.ParseInt(section.GetValueOrDefault("Arma")),
                Shield = IniValue.ParseInt(section.GetValueOrDefault("Escudo")),
                Helmet = IniValue.ParseInt(section.GetValueOrDefault("Casco")),
                Name = section.GetValueOrDefault("Name") ?? "",
                ShowName = IniValue.ParseInt(section.GetValueOrDefault("ShowName")) != 0,
            };
        }

        return catalog._npcs.Count > 0 ? catalog : null;
    }
}

public sealed class NpcDef
{
    public int Body { get; init; }
    public int Head { get; init; }
    public int Heading { get; init; } = 3;
    public int Weapon { get; init; }
    public int Shield { get; init; }
    public int Helmet { get; init; }
    public string Name { get; init; } = "";
    public bool ShowName { get; init; }
}
