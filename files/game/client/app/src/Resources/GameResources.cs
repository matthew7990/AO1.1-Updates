using System;
using System.IO;
using System.Threading.Tasks;
using Godot;

namespace Argentum.Client.Resources;

public sealed class GameResources
{
    public GrhCatalog Grhs { get; private set; } = null!;
    public BodyCatalog Bodies { get; private set; } = null!;
    public HeadCatalog Heads { get; private set; } = null!;
    public HeadCatalog? Helmets { get; private set; }
    public ObjectCatalog Objects { get; private set; } = null!;
    public ItemCatalog? Items { get; private set; }
    public MapsWorldCatalog? MapsWorld { get; private set; }
    public DirectionalGrhCatalog? Weapons { get; private set; }
    public DirectionalGrhCatalog? Shields { get; private set; }
    public NpcCatalog? Npcs { get; private set; }
    public SpellCatalog? Spells { get; private set; }
    public FxCatalog? Fxs { get; private set; }
    public TextureCache Textures { get; private set; } = null!;

    public static GameResources? TryLoad() =>
        TryLoadAsync().GetAwaiter().GetResult();

    public static async Task<GameResources?> TryLoadAsync(Func<string, int, Task>? progress = null)
    {
        static async Task Report(Func<string, int, Task>? progress, string message, int percent)
        {
            if (progress is not null)
            {
                await progress(message, percent);
            }
        }

        await Report(progress, "Buscando recursos", 2);
        var root = ResourcesRoot.Resolve();
        if (root is null)
        {
            return null;
        }
        try
        {
            await Report(progress, "Leyendo catalogo grafico", 8);
            GD.Print($"Loading AO resources from {root}");
            var grhs = GrhCatalog.Load(root);
            GD.Print($"Grh catalog: {grhs.Capacity} slots (Graficos.ini / graficos.ind)");
            await Report(progress, "Cargando cuerpos", 22);
            var molds = MoldCatalog.Load(root);
            var bodies = BodyCatalog.Load(root, molds);
            await Report(progress, "Cargando cabezas y cascos", 34);
            var heads = HeadCatalog.Load(root);
            var helmets = HeadCatalog.Load(root, "cascos.ind");
            await Report(progress, "Preparando texturas", 46);
            var textures = new TextureCache(root);
            heads.ResolveDirectionFallbacks(grhs, textures);
            helmets.ResolveDirectionFallbacks(grhs, textures);
            heads.LogSample(1);
            await Report(progress, "Cargando objetos", 58);
            var objects = ObjectCatalog.Load(root);
            var items = ItemCatalog.TryLoad(root);
            await Report(progress, "Cargando mapas", 70);
            var mapsWorld = MapsWorldCatalog.TryLoad(root);
            await Report(progress, "Cargando equipo", 80);
            var weapons = DirectionalGrhCatalog.TryLoad(root, "armas.dat", "NumArmas", "ARMA");
            var shields = DirectionalGrhCatalog.TryLoad(root, "escudos.dat", "NumEscudos", "ESC");
            await Report(progress, "Cargando NPCs y hechizos", 90);
            var npcs = NpcCatalog.TryLoad(root);
            var spells = SpellCatalog.TryLoad(root);
            var fxs = FxCatalog.TryLoad(root);
            if (mapsWorld is not null)
            {
                GD.Print($"MapsWorld catalog loaded ({mapsWorld.WorldCount} worlds)");
            }
            else
            {
                GD.PushWarning("mapsworlddata.dat not loaded — adjacent maps disabled");
            }
            await Report(progress, "Listo", 100);
            return new GameResources
            {
                Grhs = grhs,
                Bodies = bodies,
                Heads = heads,
                Helmets = helmets,
                Objects = objects,
                Items = items,
                Textures = textures,
                MapsWorld = mapsWorld,
                Weapons = weapons,
                Shields = shields,
                Npcs = npcs,
                Spells = spells,
                Fxs = fxs,
            };
        }
        catch (Exception exception)
        {
            GD.PushError($"Failed to load AO resources: {exception.Message}");
            return null;
        }
    }
}
