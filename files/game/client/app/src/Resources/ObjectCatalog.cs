using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Argentum.Client.Resources;

public sealed class ObjDef
{
    public int GrhIndex;
    public int ObjType;
}

/// <summary>Objetos del mapa (puertas, carteles, árboles…) desde localindex.dat → ObjData.GrhIndex.</summary>
public sealed class ObjectCatalog
{
    private readonly ObjDef?[] _objects;

    public ObjectCatalog(ObjDef?[] objects) => _objects = objects;

    public ObjDef? Get(int objectIndex)
    {
        if (objectIndex <= 0 || objectIndex >= _objects.Length)
        {
            return null;
        }
        return _objects[objectIndex];
    }

    public int GetGrh(int objectIndex) => Get(objectIndex)?.GrhIndex ?? 0;

    public static ObjectCatalog Load(string root)
    {
        var path = ResolveIndexPath(root);
        if (path is null)
        {
            Godot.GD.PushWarning("ObjectCatalog: no localindex.dat found; map objects will not render.");
            return new ObjectCatalog([]);
        }
        var sections = IniSections.Load(path);
        if (!sections.TryGetValue("INIT", out var init))
        {
            return new ObjectCatalog([]);
        }
        var count = ParseInt(init.GetValueOrDefault("NumObjs"));
        if (count <= 0)
        {
            return new ObjectCatalog([]);
        }
        var objects = new ObjDef?[count + 1];
        for (var i = 1; i <= count; i++)
        {
            if (!sections.TryGetValue($"OBJ{i}", out var section))
            {
                continue;
            }
            var grh = ParseInt(section.GetValueOrDefault("grhindex"));
            if (grh <= 0)
            {
                continue;
            }
            objects[i] = new ObjDef
            {
                GrhIndex = grh,
                ObjType = ParseInt(section.GetValueOrDefault("ObjType")),
            };
        }
        Godot.GD.Print($"ObjectCatalog: {objects.Count(o => o is not null)} objects from {path}");
        return new ObjectCatalog(objects);
    }

    private static string? ResolveIndexPath(string root)
    {
        var initDir = Path.Combine(root, "init");
        if (!Directory.Exists(initDir))
        {
            return null;
        }
        var lang = System.Environment.GetEnvironmentVariable("AO_LANG") ?? "es";
        var preferred = Path.Combine(initDir, $"{lang}_localindex.dat");
        if (File.Exists(preferred))
        {
            return preferred;
        }
        var fallback = Path.Combine(initDir, "es_localindex.dat");
        if (File.Exists(fallback))
        {
            return fallback;
        }
        return Directory.GetFiles(initDir, "*localindex.dat").FirstOrDefault();
    }

    private static int ParseInt(string? value) =>
        string.IsNullOrWhiteSpace(value) ? 0 :
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
}
