using System;
using System.Collections.Generic;
using Argentum.Client.Resources;

namespace Argentum.Client.World;

/// <summary>Precarga mapas destino referenciados por tiles ExitMap del mapa actual.</summary>
public sealed class AdjacentMapRing
{
    public const float SliceFraction = 0.55f;

    public readonly struct NeighborSlice
    {
        public CsmMap Map { get; init; }
        public int ExitX { get; init; }
        public int ExitY { get; init; }
        public int SpawnX { get; init; }
        public int SpawnY { get; init; }
        public int SrcMinX { get; init; }
        public int SrcMaxX { get; init; }
        public int SrcMinY { get; init; }
        public int SrcMaxY { get; init; }
        public int VirtMinX { get; init; }
        public int VirtMaxX { get; init; }
        public int VirtMinY { get; init; }
        public int VirtMaxY { get; init; }
        public int FaceDirX { get; init; }
        public int FaceDirY { get; init; }
    }

    private readonly Dictionary<int, CsmMap?> _maps = new();

    public void PreloadFromCuts(MapWarpCutIndex cuts)
    {
        _maps.Clear();
        foreach (var cut in cuts.Cuts)
        {
            if (_maps.ContainsKey(cut.DestMapId))
            {
                continue;
            }
            _maps[cut.DestMapId] = MapLoader.TryLoad(cut.DestMapId, cut.DestMapId);
        }
    }
}
