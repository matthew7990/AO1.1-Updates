using System;
using System.Collections.Generic;
using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.World;

/// <summary>Genera nombres y líneas de modificadores estilo PoE a partir de elemental tags del servidor.</summary>
public static class ItemAffixes
{
    public const int ElementFire = 1;
    public const int ElementWater = 2;
    public const int ElementEarth = 4;
    public const int ElementWind = 8;
    public const int ElementLight = 16;
    public const int ElementDark = 32;
    public const int ElementChaos = 64;
    public const int RarityNormal = 0;
    public const int RarityMagic = 1;
    public const int RarityRare = 2;
    public const int RarityEpic = 3;
    public const int RarityLegendary = 4;

    private const int ElementBits = 8;
    private const int RarityBits = 4;
    private const int SeedShift = ElementBits + RarityBits;
    private const int RarityMask = 0xF;

    private static readonly string[] Prefixes =
    [
        "Ardiente", "Gélida", "Salvaje", "Precisa", "Robusta", "Veloz", "Maldita", "Sagrada",
    ];

    private static readonly string[] Suffixes =
    [
        "fuego", "hielo", "tormenta", "precisión", "vida", "sombra", "caos", "el alba",
        "la noche", "el viento", "la tierra", "el mar", "los ancestros", "la caza",
    ];

    private const int ObjWeapon = 2;
    private const int ObjArmor = 3;
    private const int ObjShield = 16;
    private const int ObjHelmet = 17;
    private const int ObjMagic = 21;
    private const int ObjMagicResist = 35;

    private static readonly string[] PassiveLines =
    [
        "+{0} vida máxima",
        "+{0} maná máxima",
        "+{0}% daño elemental",
        "Regenera {0} vida cada 8 s",
        "+{0} evasión",
        "+{0} resistencia mágica",
        "Inmune a veneno",
        "+{0}% velocidad de ataque",
    ];

    private static readonly string[] WeaponLines =
    [
        "+{0}% dano fisico",
        "+{0}% velocidad de ataque",
        "+{0} precision",
        "+{0}% dano elemental",
    ];

    private static readonly string[] ArmorLines =
    [
        "+{0} defensa",
        "+{0} vida maxima",
        "+{0} resistencia elemental",
        "Reduce {0}% el dano recibido",
    ];

    private static readonly string[] ShieldLines =
    [
        "+{0} bloqueo",
        "+{0} defensa",
        "+{0} resistencia magica",
        "Reduce {0}% el dano frontal",
    ];

    private static readonly string[] HelmetLines =
    [
        "+{0} defensa",
        "+{0} evasion",
        "+{0} resistencia magica",
        "Reduce {0}% efectos de control",
    ];

    private static readonly string[] MagicLines =
    [
        "+{0} mana maxima",
        "+{0}% poder magico",
        "+{0} resistencia magica",
        "Regenera {0} mana cada 8 s",
    ];

    public static int ElementMask(int tags) => tags & 0xFF;

    public static int AffixSeed(int tags) => (tags >> SeedShift) & 0xFFFFF;

    public static int Rarity(int tags)
    {
        var rarity = (tags >> ElementBits) & RarityMask;
        return tags != 0 && rarity == 0 ? RarityMagic : rarity;
    }

    public static bool HasModifiers(int tags) => tags != 0;

    public static string RarityLabel(int tags) => Rarity(tags) switch
    {
        RarityLegendary => "Legendario",
        RarityEpic => "Epico",
        RarityRare => "Raro",
        RarityMagic => "Magico",
        _ => "Normal",
    };

    public static int EffectCount(ItemDef? item, int tags)
    {
        if (!HasModifiers(tags))
        {
            return 0;
        }
        var count = Rarity(tags);
        if (item?.ObjType == ObjWeapon && count > 4)
        {
            count = 4;
        }
        return Math.Min(count, 5);
    }

    public static int RequiredLevel(ItemDef? item, int tags)
    {
        var req = Math.Max(1, item?.MinLevel ?? 1);
        if (!HasModifiers(tags))
        {
            return req;
        }
        var seed = AffixSeed(tags);
        var bonus = EffectCount(item, tags) - 1;
        if (Rarity(tags) >= RarityEpic || seed % 100 < 12)
        {
            bonus++;
        }
        if (Rarity(tags) >= RarityLegendary || seed % 100 < 4)
        {
            bonus++;
        }
        return req + bonus * 2;
    }

    public static string BuildDisplayName(ItemDef? item, int tags)
    {
        var baseName = item?.Name;
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "Objeto";
        }
        if (!HasModifiers(tags))
        {
            return baseName;
        }
        var seed = AffixSeed(tags);
        var prefix = PrefixFor(tags, seed);
        var suffix = Suffixes[seed % Suffixes.Length];
        if (!string.IsNullOrEmpty(prefix))
        {
            return $"{StripPlusTier(baseName)} {prefix.ToLowerInvariant()} de {suffix}";
        }
        return $"{StripPlusTier(baseName)} de {suffix}";
    }

    public static IReadOnlyList<string> BuildModifierLines(ItemDef? item, int tags)
    {
        var lines = new List<string>();
        if (!HasModifiers(tags))
        {
            if (item is { MinHit: > 0 } or { MaxHit: > 0 })
            {
                lines.Add($"Daño: {item.MinHit}-{item.MaxHit}");
            }
            if (item is { Valor: > 0 })
            {
                lines.Add($"Valor: {item.Valor}");
            }
            return lines;
        }
        var seed = AffixSeed(tags);
        foreach (var elementLine in ElementLinesFor(ElementMask(tags), item?.ObjType ?? 0))
        {
            lines.Add(elementLine);
        }
        var passivePool = PassivePoolFor(item?.ObjType ?? 0);
        var passiveVal = PassiveValueFor(item?.ObjType ?? 0, seed);
        var count = Math.Max(1, EffectCount(item, tags));
        for (var i = 0; i < count; i++)
        {
            var passiveIdx = (seed >> (3 + i * 3)) % passivePool.Length;
            var val = passiveVal + i * 2;
            var line = string.Format(passivePool[passiveIdx], val);
            if (!lines.Contains(line))
            {
                lines.Add(line);
            }
        }
        if (item is { MinHit: > 0 } or { MaxHit: > 0 })
        {
            var bonus = 1 + (seed % 4);
            lines.Add($"Daño: {item.MinHit + bonus}-{item.MaxHit + bonus}");
        }
        else if (item is { Valor: > 0 })
        {
            lines.Add($"Valor base: {item.Valor}");
        }
        return lines;
    }

    public static Color TitleColor(int tags)
    {
        return ElementMask(tags) switch
        {
            ElementFire => new Color("ff8a4a"),
            ElementWater => new Color("6ab8ff"),
            ElementEarth => new Color("c4a060"),
            ElementWind => new Color("9ae8c0"),
            ElementLight => new Color("fff0a8"),
            ElementDark => new Color("b088ff"),
            ElementChaos => new Color("ff5a8a"),
            _ => Rarity(tags) switch
            {
                RarityLegendary => new Color("ff9f2e"),
                RarityEpic => new Color("c875ff"),
                RarityRare => new Color("f0d05a"),
                RarityMagic => new Color("7fb9ff"),
                _ => new Color("ebe4d6"),
            },
        };
    }

    public static bool IsEquipmentType(int objType) => objType is ObjWeapon or ObjArmor or ObjShield or ObjHelmet or ObjMagic;

    public static string TypeLabel(int objType) => objType switch
    {
        ObjWeapon => "Arma",
        ObjArmor => "Armadura",
        ObjShield => "Escudo",
        ObjHelmet => "Casco",
        ObjMagic or ObjMagicResist => "Accesorio",
        _ => "Objeto",
    };

    private static string PrefixFor(int tags, int seed)
    {
        var elemental = ElementMask(tags);
        if (elemental == ElementFire)
        {
            return "Ardiente";
        }
        if (elemental == ElementWater)
        {
            return "Gélida";
        }
        if (elemental == ElementEarth)
        {
            return "Robusta";
        }
        if (elemental == ElementWind)
        {
            return "Veloz";
        }
        if (elemental == ElementLight)
        {
            return "Sagrada";
        }
        if (elemental == ElementDark)
        {
            return "Maldita";
        }
        if (elemental == ElementChaos)
        {
            return "Caótica";
        }
        return Prefixes[seed % Prefixes.Length];
    }

    private static string[] PassivePoolFor(int objType) => objType switch
    {
        ObjWeapon => WeaponLines,
        ObjArmor => ArmorLines,
        ObjShield => ShieldLines,
        ObjHelmet => HelmetLines,
        ObjMagic or ObjMagicResist => MagicLines,
        _ => PassiveLines,
    };

    private static int PassiveValueFor(int objType, int seed)
    {
        var roll = 1 + (seed % 12);
        return objType switch
        {
            ObjWeapon => 2 + roll,
            ObjArmor or ObjShield or ObjHelmet => 1 + roll,
            _ => 1 + roll,
        };
    }

    private static IEnumerable<string> ElementLinesFor(int elemental, int objType)
    {
        if ((elemental & ElementFire) != 0)
        {
            yield return objType == ObjWeapon ? "[Fuego] Quema al golpear" : "[Fuego] Resistencia al fuego";
        }
        if ((elemental & ElementWater) != 0)
        {
            yield return objType == ObjWeapon ? "[Agua] Ralentiza al golpear" : "[Agua] Mitiga ralentizaciones";
        }
        if ((elemental & ElementEarth) != 0)
        {
            yield return objType == ObjWeapon ? "[Tierra] Golpes mas pesados" : "[Tierra] Mas armadura";
        }
        if ((elemental & ElementWind) != 0)
        {
            yield return objType == ObjWeapon ? "[Aire] Ataques mas rapidos" : "[Aire] Mayor evasion";
        }
        if ((elemental & ElementLight) != 0)
        {
            yield return objType == ObjWeapon ? "[Luz] Dano extra a no-muertos" : "[Luz] Proteccion sagrada";
        }
        if ((elemental & ElementDark) != 0)
        {
            yield return objType == ObjWeapon ? "[Oscuridad] Drena al golpear" : "[Oscuridad] Resistencia oscura";
        }
        if ((elemental & ElementChaos) != 0)
        {
            yield return objType == ObjWeapon ? "[Caos] Dano ignora resistencias" : "[Caos] Resistencia al caos";
        }
    }

    private static IEnumerable<string> ElementLines(int elemental)
    {
        if ((elemental & ElementFire) != 0)
        {
            yield return "[Fuego] Añade daño ígneo";
        }
        if ((elemental & ElementWater) != 0)
        {
            yield return "[Agua] Ralentiza al golpear";
        }
        if ((elemental & ElementEarth) != 0)
        {
            yield return "[Tierra] Más armadura";
        }
        if ((elemental & ElementWind) != 0)
        {
            yield return "[Aire] Mayor evasión";
        }
        if ((elemental & ElementLight) != 0)
        {
            yield return "[Luz] Daño extra a no-muertos";
        }
        if ((elemental & ElementDark) != 0)
        {
            yield return "[Oscuridad] Roba vida";
        }
        if ((elemental & ElementChaos) != 0)
        {
            yield return "[Caos] Daño ignora resistencias";
        }
    }

    private static string StripPlusTier(string name)
    {
        var idx = name.IndexOf('+');
        return idx > 0 ? name[..idx].TrimEnd() : name;
    }
}
