using System.Collections.Generic;

namespace Argentum.Client.World;

/// <summary>NPC vivo en mapa adyacente (posición desde servidor).</summary>
public readonly struct AdjacentPeekLiveNpc
{
    public int MapId { get; init; }
    public int SrcX { get; init; }
    public int SrcY { get; init; }
    public int Body { get; init; }
    public int Head { get; init; }
    public int Heading { get; init; }
    public int Weapon { get; init; }
    public int Shield { get; init; }
    public int Helmet { get; init; }
    public int MinHp { get; init; }
    public int MaxHp { get; init; }
    public string Name { get; init; }
}

public sealed class AdjacentPeekLiveState
{
    private readonly Dictionary<int, List<AdjacentPeekLiveNpc>> _byMap = new();
    private ulong _requestSignature;
    private long _lastRequestTicks;
    private int _version;

    public int Version => _version;

    public const int MinRefreshMs = 900;

    public bool ShouldRequest(ulong signature, long nowTicks)
    {
        if (signature == 0)
        {
            return false;
        }
        if (signature != _requestSignature)
        {
            return true;
        }
        return nowTicks - _lastRequestTicks >= MinRefreshMs;
    }

    public void MarkRequested(ulong signature, long nowTicks)
    {
        _requestSignature = signature;
        _lastRequestTicks = nowTicks;
    }

    public void SetMapNpcs(int mapId, IReadOnlyList<AdjacentPeekLiveNpc> npcs)
    {
        _byMap[mapId] = new List<AdjacentPeekLiveNpc>(npcs);
        _version++;
    }

    public void ClearMap(int mapId) => _byMap.Remove(mapId);

    public IEnumerable<AdjacentPeekLiveNpc> ForMap(int mapId) =>
        _byMap.TryGetValue(mapId, out var list) ? list : [];
}
