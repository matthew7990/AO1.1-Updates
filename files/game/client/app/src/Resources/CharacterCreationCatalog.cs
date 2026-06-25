using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Argentum.Client.Resources;

public readonly struct RaceModifiers
{
    public int Fuerza { get; init; }
    public int Agilidad { get; init; }
    public int Inteligencia { get; init; }
    public int Constitucion { get; init; }
    public int Carisma { get; init; }
}

public sealed class CharacterCreationCatalog
{
  public const int BaseAttribute = 18;
  public const int MaxCharactersPerAccount = 10;

  public IReadOnlyList<string> Races { get; }
  public IReadOnlyList<string> Classes { get; }
  public IReadOnlyList<string> Cities { get; }
  public IReadOnlyList<string> Genders { get; } = ["Hombre", "Mujer"];

  private readonly Dictionary<string, RaceModifiers> _raceModifiers;
  private readonly Dictionary<string, JsonRace> _bodyData;

  private CharacterCreationCatalog(
      IReadOnlyList<string> races,
      IReadOnlyList<string> classes,
      IReadOnlyList<string> cities,
      Dictionary<string, RaceModifiers> raceModifiers,
      Dictionary<string, JsonRace> bodyData)
  {
    Races = races;
    Classes = classes;
    Cities = cities;
    _raceModifiers = raceModifiers;
    _bodyData = bodyData;
  }

  public static CharacterCreationCatalog Load(string resourcesRoot)
  {
    var races = new[]
    {
      "", "Humano", "Elfo", "Elfo Oscuro", "Gnomo", "Enano", "Orco",
    };
    var classes = new[]
    {
      "",
      "Mago", "Clérigo", "Guerrero", "Asesino", "Bardo", "Druida",
      "Paladín", "Cazador", "Trabajador", "Pirata", "Ladrón", "Bandido",
    };
    var cities = new[]
    {
      "",
      "Ullathorpe", "Nix", "Banderbill", "Lindos", "Arghal", "Arkhein", "Forgat",
      "Eldoria", "Penthar", "Morgrim",
    };

    var sections = IniSections.Load(Path.Combine(resourcesRoot, "init", "localindex.dat"));
    var modRaza = sections.GetValueOrDefault("MODRAZA") ?? new Dictionary<string, string>();
    var raceModifiers = new Dictionary<string, RaceModifiers>(StringComparer.OrdinalIgnoreCase)
    {
      ["Humano"] = ReadRace(modRaza, "HUMANO"),
      ["Elfo"] = ReadRace(modRaza, "ELFO"),
      ["ElfoOscuro"] = ReadRace(modRaza, "ELFOOSCURO"),
      ["Gnomo"] = ReadRace(modRaza, "GNOMO"),
      ["Enano"] = ReadRace(modRaza, "ENANO"),
      ["Orco"] = ReadRace(modRaza, "ORCO"),
    };

    var bodyPath = Path.Combine(resourcesRoot, "init", "HeadAndBodyData.json");
    var bodyData = JsonSerializer.Deserialize<Dictionary<string, JsonRace>>(
        File.ReadAllText(bodyPath),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
      ?? new Dictionary<string, JsonRace>();

    return new CharacterCreationCatalog(races, classes, cities, raceModifiers, bodyData);
  }

  public RaceModifiers GetRaceModifiers(int raceIndex)
  {
    var key = raceIndex switch
    {
      1 => "Humano",
      2 => "Elfo",
      3 => "ElfoOscuro",
      4 => "Gnomo",
      5 => "Enano",
      6 => "Orco",
      _ => "Humano",
    };
    return _raceModifiers.GetValueOrDefault(key);
  }

  public int GetBodyId(int raceIndex, bool male)
  {
    var key = raceIndex switch
    {
      1 => "Human",
      2 => "Elf",
      3 => "Drow",
      4 => "Gnome",
      5 => "Dwarf",
      6 => "Orc",
      _ => "Human",
    };
    if (!_bodyData.TryGetValue(key, out var race))
    {
      return male ? 21 : 39;
    }
  var gender = male ? race.Male : race.Female;
    return gender?.Body ?? (male ? 21 : 39);
  }

  /// <summary>VB6 ModCabezas.DameOpciones — cabezas válidas por raza/género.</summary>
  public IReadOnlyList<int> GetHeadOptions(int raceIndex, int genderIndex)
  {
    var male = genderIndex == 1;
    return (raceIndex, male) switch
    {
      (1, true) => Range(1, 41).Concat(Range(778, 791)).ToArray(),
      (1, false) => Range(50, 80).Concat(Range(187, 190)).Concat(Range(230, 246)).ToArray(),
      (2, true) => Range(101, 132).Concat(Range(531, 545)).ToArray(),
      (2, false) => Range(150, 179).Concat(Range(758, 777)).ToArray(),
      (3, true) => Range(200, 229).Concat(Range(792, 810)).ToArray(),
      (3, false) => Range(250, 279).ToArray(),
      (4, true) => Range(400, 429).ToArray(),
      (4, false) => Range(450, 479).ToArray(),
      (5, true) => Range(300, 344).ToArray(),
      (5, false) => Range(350, 379).ToArray(),
      (6, true) => Range(500, 529).ToArray(),
      (6, false) => Range(550, 579).ToArray(),
      _ => [male ? 1 : 50],
    };
  }

  public bool IsCreationComplete(int race, int gender, int classId, int head, int home) =>
    race > 0 && gender > 0 && classId > 0 && head > 0 && home > 0;

  private static RaceModifiers ReadRace(Dictionary<string, string> section, string prefix) => new()
  {
    Fuerza = Parse(section, prefix + "FUERZA"),
    Agilidad = Parse(section, prefix + "AGILIDAD"),
    Inteligencia = Parse(section, prefix + "INTELIGENCIA"),
    Constitucion = Parse(section, prefix + "CONSTITUCION"),
    Carisma = Parse(section, prefix + "CARISMA"),
  };

  private static int Parse(Dictionary<string, string> section, string key) =>
    int.TryParse(section.GetValueOrDefault(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

  private static IEnumerable<int> Range(int from, int to)
  {
    for (var i = from; i <= to; i++)
    {
      yield return i;
    }
  }

  private sealed class JsonRace
  {
    public JsonGender? Male { get; set; }
    public JsonGender? Female { get; set; }
  }

  private sealed class JsonGender
  {
    public int Body { get; set; }
  }
}
