using System.Collections.Generic;

namespace Argentum.Client.World;

/// <summary>VB6 RoofsLight() — alpha por tipo de trigger al entrar/salir de techos.</summary>
public sealed class RoofFadeState
{
    // VB6: timerElapsedTime(ms) * engineBaseSpeed(0.018) * 48
    private const float FadePerSecond = 864f;

    private readonly Dictionary<short, float> _alpha = new();

    public void ResetForMap(CsmMap? map)
    {
        _alpha.Clear();
        if (map is null)
        {
            return;
        }
        for (var x = CsmMap.MinMapTile; x <= CsmMap.MaxMapTile; x++)
        {
            for (var y = CsmMap.MinMapTile; y <= CsmMap.MaxMapTile; y++)
            {
                var trigger = map.Tiles[x, y].Trigger;
                if (MapTrigger.HayTecho(trigger))
                {
                    _alpha.TryAdd(trigger, 255f);
                }
            }
        }
    }

    public void Advance(double deltaSeconds, short playerTrigger)
    {
        if (_alpha.Count == 0)
        {
            return;
        }
        var step = (float)deltaSeconds * FadePerSecond;
        var keys = new List<short>(_alpha.Keys);
        foreach (var trigger in keys)
        {
            if (trigger == playerTrigger)
            {
                if (_alpha[trigger] > 0f)
                {
                    _alpha[trigger] -= step;
                    if (_alpha[trigger] < 0f)
                    {
                        _alpha[trigger] = 0f;
                    }
                }
            }
            else if (_alpha[trigger] < 255f)
            {
                _alpha[trigger] += step;
                if (_alpha[trigger] > 255f)
                {
                    _alpha[trigger] = 255f;
                }
            }
        }
    }

    public float GetAlpha255(short trigger)
    {
        if (trigger == 0 || !_alpha.TryGetValue(trigger, out var alpha))
        {
            return 255f;
        }
        return alpha;
    }
}
