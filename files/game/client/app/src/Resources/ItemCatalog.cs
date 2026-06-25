using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;

namespace Argentum.Client.Resources;

public sealed class ItemDef
{
    public string Name = "";
    public string Texto = "";
    public int GrhIndex;
    public int ObjType;
    public int Valor;
    public int MinHit;
    public int MaxHit;
    public int MinLevel;
    public int MaxLevel;
    public int Agarrable;
}

/// <summary>Ítems de inventario desde Dat/obj.dat (VB6 ObjData).</summary>
public sealed class ItemCatalog
{
    private readonly ItemDef?[] _items;

    public ItemCatalog(ItemDef?[] items) => _items = items;

    public ItemDef? Get(int itemIndex)
    {
        if (itemIndex <= 0 || itemIndex >= _items.Length)
        {
            return null;
        }
        return _items[itemIndex];
    }

    public int GetGrh(int itemIndex) => Get(itemIndex)?.GrhIndex ?? 0;

    public string GetName(int itemIndex) => Get(itemIndex)?.Name ?? $"Obj {itemIndex}";

    public string GetTexto(int itemIndex) => Get(itemIndex)?.Texto ?? "";

    public int GetValor(int itemIndex) => Get(itemIndex)?.Valor ?? 0;

    public static ItemCatalog? TryLoad(string root)
    {
        var path = Path.Combine(root, "Dat", "obj.dat");
        if (!File.Exists(path))
        {
            GD.PushWarning($"ItemCatalog: no obj.dat en {path}");
            return null;
        }
        try
        {
            var sections = IniSections.Load(path);
            if (!sections.TryGetValue("INIT", out var init))
            {
                return null;
            }
            var count = GetInt(init, "NumOBJs");
            if (count <= 0)
            {
                count = GetInt(init, "NumObjs");
            }
            if (count <= 0)
            {
                return null;
            }
            var items = new ItemDef?[count + 1];
            for (var i = 1; i <= count; i++)
            {
                if (!sections.TryGetValue($"OBJ{i}", out var section))
                {
                    continue;
                }
                var grh = GetInt(section, "GrhIndex");
                if (grh <= 0)
                {
                    grh = GetInt(section, "grhindex");
                }
                section.TryGetValue("Name", out var name);
                if (string.IsNullOrWhiteSpace(name) && section.TryGetValue("en_name", out var enName))
                {
                    name = enName;
                }
                section.TryGetValue("Texto", out var texto);
                if (string.IsNullOrWhiteSpace(texto) && section.TryGetValue("en_texto", out var enTexto))
                {
                    texto = enTexto;
                }
                items[i] = new ItemDef
                {
                    Name = (name ?? "").Trim(),
                    Texto = (texto ?? "").Trim(),
                    GrhIndex = grh,
                    ObjType = GetInt(section, "ObjType"),
                    Valor = GetInt(section, "Valor"),
                    MinHit = GetInt(section, "MinHIT"),
                    MaxHit = GetInt(section, "MaxHIT"),
                    MinLevel = GetInt(section, "MinELV"),
                    MaxLevel = GetInt(section, "MaxLEV"),
                    Agarrable = GetInt(section, "Agarrable"),
                };
            }
            GD.Print($"ItemCatalog: {count} objetos desde obj.dat");
            return new ItemCatalog(items);
        }
        catch (Exception ex)
        {
            GD.PushError($"ItemCatalog: {ex.Message}");
            return null;
        }
    }

    private static int GetInt(Dictionary<string, string> section, string key) =>
        section.TryGetValue(key, out var value) ? ParseInt(value) : 0;

    private static int ParseInt(string? value) =>
        string.IsNullOrWhiteSpace(value) ? 0 :
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
}
