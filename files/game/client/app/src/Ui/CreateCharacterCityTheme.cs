using Godot;

namespace Argentum.Client.Ui;

/// <summary>Tema visual y spawn por ciudad natal (alineado con domain.CitySpawns del servidor).</summary>
public readonly struct CreateCharacterCityTheme
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Tagline { get; init; }
    public int MapId { get; init; }
    public int SpawnX { get; init; }
    public int SpawnY { get; init; }
    public Color SkyTop { get; init; }
    public Color SkyBottom { get; init; }
    public Color Accent { get; init; }
    public Color Mist { get; init; }

    public static CreateCharacterCityTheme ForId(int cityId)
    {
        foreach (var theme in All)
        {
            if (theme.Id == cityId)
            {
                return theme;
            }
        }
        return All[0];
    }

    public static readonly CreateCharacterCityTheme[] All =
    [
        new()
        {
            Id = 1, Name = "Ullathorpe", Tagline = "Cuna de aventureros entre campos dorados",
            MapId = 1, SpawnX = 57, SpawnY = 44,
            SkyTop = new Color("4a7ab8"), SkyBottom = new Color("c9a85c"),
            Accent = new Color("d9a441"), Mist = new Color("8ab87a", 0.35f),
        },
        new()
        {
            Id = 2, Name = "Nix", Tagline = "La capital imperial bajo cielos fríos",
            MapId = 34, SpawnX = 40, SpawnY = 87,
            SkyTop = new Color("2c3e5c"), SkyBottom = new Color("6a7a94"),
            Accent = new Color("a8c4e8"), Mist = new Color("8899bb", 0.42f),
        },
        new()
        {
            Id = 3, Name = "Banderbill", Tagline = "Fortaleza roja en tierras áridas",
            MapId = 59, SpawnX = 47, SpawnY = 41,
            SkyTop = new Color("8b3a2a"), SkyBottom = new Color("d4845a"),
            Accent = new Color("e85a4f"), Mist = new Color("c47850", 0.38f),
        },
        new()
        {
            Id = 4, Name = "Lindos", Tagline = "Puerto soleado y brisa salada",
            MapId = 408, SpawnX = 63, SpawnY = 39,
            SkyTop = new Color("3a9ec4"), SkyBottom = new Color("7ed4e8"),
            Accent = new Color("5ec9b0"), Mist = new Color("6ec8d8", 0.32f),
        },
        new()
        {
            Id = 5, Name = "Arghal", Tagline = "Ciudadela del norte entre niebla y piedra",
            MapId = 151, SpawnX = 61, SpawnY = 43,
            SkyTop = new Color("3d3560"), SkyBottom = new Color("6b6088"),
            Accent = new Color("9b8fd4"), Mist = new Color("7a7098", 0.45f),
        },
        new()
        {
            Id = 6, Name = "Arkhein", Tagline = "Dominios oscuros al borde del imperio",
            MapId = 196, SpawnX = 43, SpawnY = 58,
            SkyTop = new Color("1a1a28"), SkyBottom = new Color("4a4058"),
            Accent = new Color("b06a9a"), Mist = new Color("504060", 0.5f),
        },
        new()
        {
            Id = 7, Name = "Forgat", Tagline = "Horno eterno y tierra volcánica",
            MapId = 517, SpawnX = 48, SpawnY = 65,
            SkyTop = new Color("4a1810"), SkyBottom = new Color("c45a28"),
            Accent = new Color("ff8a3c"), Mist = new Color("e06030", 0.4f),
        },
        new()
        {
            Id = 8, Name = "Eldoria", Tagline = "Bosques antiguos y magia ancestral",
            MapId = 440, SpawnX = 52, SpawnY = 88,
            SkyTop = new Color("1e4a32"), SkyBottom = new Color("5a9a68"),
            Accent = new Color("7ed67a"), Mist = new Color("4a8860", 0.38f),
        },
        new()
        {
            Id = 9, Name = "Penthar", Tagline = "Torres de observación en la meseta",
            MapId = 560, SpawnX = 40, SpawnY = 69,
            SkyTop = new Color("5a6a88"), SkyBottom = new Color("a8b8d0"),
            Accent = new Color("c8d8f0"), Mist = new Color("98a8c8", 0.36f),
        },
        new()
        {
            Id = 10, Name = "Morgrim", Tagline = "Murallas de hierro en tierra hostil",
            MapId = 591, SpawnX = 50, SpawnY = 50,
            SkyTop = new Color("2a2a32"), SkyBottom = new Color("5a5a68"),
            Accent = new Color("a0a8b8"), Mist = new Color("686878", 0.44f),
        },
    ];
}
