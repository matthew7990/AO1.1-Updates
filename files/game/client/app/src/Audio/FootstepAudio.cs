using Argentum.Client.Resources;
using Argentum.Client.World;

namespace Argentum.Client.Audio;

/// <summary>VB6 TileEngine.DoPasosFx / GetTerrenoDePaso / Recursos.CargarPasos.</summary>
public static class FootstepAudio
{
    private enum TerrainStep
    {
        Forest = 1,
        Snow = 2,
        Horse = 3,
        Dungeon = 4,
        Desert = 5,
        Floor = 6,
        Water = 7,
    }

    private static readonly int[][] TerrainWavs =
    [
        [],
        [AoSoundIndex.PasoBosque1, AoSoundIndex.PasoPiso69],
        [AoSoundIndex.PasoNieve1, AoSoundIndex.PasoNieve2],
        [AoSoundIndex.PasoCaballo1, AoSoundIndex.PasoCaballo2],
        [AoSoundIndex.Paso1, AoSoundIndex.Paso2],
        [AoSoundIndex.PasoDesierto1, AoSoundIndex.PasoDesierto2],
        [AoSoundIndex.Paso1, AoSoundIndex.Paso2],
        [AoSoundIndex.Navegando, AoSoundIndex.Navegando],
    ];

    public static void PlayForCharacter(
        AoAudio audio,
        WorldSession session,
        GrhCatalog? grhs,
        WorldCharacter character,
        bool sailing = false)
    {
        if (!audio.StepsEnabled || character.IsNpc || session.IsDead)
        {
            return;
        }
        if (!SpatialAudio.IsInAudibleArea(character.TileX, character.TileY, session))
        {
            return;
        }
        character.StepPhase = !character.StepPhase;
        var stepIndex = character.StepPhase ? 0 : 1;
        var wave = sailing
            ? AoSoundIndex.Navegando
            : ResolveTerrainWave(grhs, session.Map, character.TileX, character.TileY, stepIndex);
        if (wave <= 0)
        {
            return;
        }
        var label = sailing ? $"sailing_{character.CharIndex}" : null;
        audio.PlayWave(
            wave,
            character.TileX,
            character.TileY,
            AoAudio.FxCategory.Steps,
            label,
            loop: sailing);
    }

    public static void PlayFromNetwork(
        AoAudio audio,
        WorldSession session,
        GrhCatalog? grhs,
        int grh1,
        int grh2,
        int distance,
        int balance,
        bool step)
    {
        if (!audio.StepsEnabled)
        {
            return;
        }
        var fileNum = grhs?.Get(grh1)?.FileNum ?? 0;
        var terrain = ResolveTerrain(fileNum, grh2);
        var stepIndex = step ? 0 : 1;
        if (terrain < 1 || terrain >= TerrainWavs.Length)
        {
            return;
        }
        var wav = TerrainWavs[terrain][stepIndex];
        var volumeDb = SpatialAudio.VolumeDb(AoAudio.FxCategory.Steps, distance, audio.StepsVolumeDb);
        var pan = SpatialAudio.Pan(balance < 0 ? session.TileX - distance : session.TileX + distance, session.TileX, distance);
        audio.PlayWaveDirect(wav, volumeDb, pan);
    }

    private static int ResolveTerrainWave(GrhCatalog? grhs, CsmMap? map, int x, int y, int stepIndex)
    {
        if (map is null || x < CsmMap.MinMapTile || y < CsmMap.MinMapTile || x > CsmMap.MaxMapTile || y > CsmMap.MaxMapTile)
        {
            return TerrainWavs[(int)TerrainStep.Floor][stepIndex];
        }
        var layer1 = map.Tiles[x, y].Graphics[0];
        var layer2 = map.Tiles[x, y].Graphics[1];
        var fileNum = grhs?.Get(layer1)?.FileNum ?? 0;
        var terrain = ResolveTerrain(fileNum, layer2);
        if (terrain < 1 || terrain >= TerrainWavs.Length)
        {
            terrain = (int)TerrainStep.Floor;
        }
        return TerrainWavs[terrain][stepIndex];
    }

    private static int ResolveTerrain(int terrainFileNum, int layer2Grh)
    {
        if ((terrainFileNum >= 6000 && terrainFileNum <= 6004)
            || (terrainFileNum >= 550 && terrainFileNum <= 552)
            || (terrainFileNum >= 6018 && terrainFileNum <= 6020))
        {
            return (int)TerrainStep.Forest;
        }
        if ((terrainFileNum >= 7501 && terrainFileNum <= 7507)
            || terrainFileNum is 7500 or 7508 or 1533 or 2508)
        {
            return (int)TerrainStep.Dungeon;
        }
        if (terrainFileNum is >= 7701 and <= 7707 or 7601 or 7602)
        {
            return (int)TerrainStep.Snow;
        }
        if (terrainFileNum is >= 7001 and <= 7010 or 12548)
        {
            return (int)TerrainStep.Desert;
        }
        if (layer2Grh is >= 1505 and <= 1509)
        {
            return (int)TerrainStep.Water;
        }
        return (int)TerrainStep.Floor;
    }
}
