using System;
using System.IO;
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

    public static GameResources? TryLoad()
    {
        var root = ResourcesRoot.Resolve();
        if (root is null)
        {
            return null;
        }
        try
        {
            GD.Print($"Loading AO resources from {root}");
            var grhs = GrhCatalog.Load(root);
            GD.Print($"Grh catalog: {grhs.Capacity} slots (Graficos.ini / graficos.ind)");
            var molds = MoldCatalog.Load(root);
            var bodies = BodyCatalog.Load(root, molds);
            var heads = HeadCatalog.Load(root);
            var helmets = HeadCatalog.Load(root, "cascos.ind");
            var textures = new TextureCache(root);
            heads.ResolveDirectionFallbacks(grhs, textures);
            helmets.ResolveDirectionFallbacks(grhs, textures);
            heads.LogSample(1);
            var objects = ObjectCatalog.Load(root);
            var items = ItemCatalog.TryLoad(root);
            var mapsWorld = MapsWorldCatalog.TryLoad(root);
            var weapons = DirectionalGrhCatalog.TryLoad(root, "armas.dat", "NumArmas", "ARMA");
            var shields = DirectionalGrhCatalog.TryLoad(root, "escudos.dat", "NumEscudos", "ESC");
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
