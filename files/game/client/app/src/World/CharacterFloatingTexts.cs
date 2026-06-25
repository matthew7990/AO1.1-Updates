using System.Collections.Generic;
using Godot;

namespace Argentum.Client.World;

/// <summary>VB6 charlist().DialogEffects vía SetCharacterDialogFx / eTextOverChar.</summary>
public sealed class CharacterFloatingTexts
{
    public readonly struct Fx
    {
        public string Text { get; init; }
        public Color Color { get; init; }
        public double StartMs { get; init; }

        public float RiseOffset(double nowMs) => (float)((nowMs - StartMs) * 0.025);

        public float Alpha(double nowMs)
        {
            const double lifeMs = 1300;
            var elapsed = nowMs - StartMs;
            if (elapsed >= lifeMs)
            {
                return 0f;
            }
            if (elapsed > 900)
            {
                return (float)((lifeMs - elapsed) * 0.0025);
            }
            return 1f;
        }

        public bool IsAlive(double nowMs) => nowMs - StartMs < 1300;
    }

    private readonly Dictionary<short, List<Fx>> _byChar = new();

    public void Add(short charIndex, string text, Color color)
    {
        if (charIndex <= 0 || string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        if (!_byChar.TryGetValue(charIndex, out var list))
        {
            list = new List<Fx>(2);
            _byChar[charIndex] = list;
        }
        list.Add(new Fx
        {
            Text = text.Trim(),
            Color = color,
            StartMs = Time.GetTicksMsec(),
        });
    }

    public IReadOnlyList<Fx> GetActive(short charIndex, double nowMs)
    {
        if (!_byChar.TryGetValue(charIndex, out var list) || list.Count == 0)
        {
            return [];
        }
        var alive = 0;
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].IsAlive(nowMs))
            {
                alive++;
            }
        }
        if (alive == 0)
        {
            _byChar.Remove(charIndex);
            return [];
        }
        return list;
    }

    public void PruneExpired()
    {
        var now = Time.GetTicksMsec();
        List<short>? remove = null;
        foreach (var (idx, list) in _byChar)
        {
            list.RemoveAll(fx => !fx.IsAlive(now));
            if (list.Count == 0)
            {
                remove ??= new List<short>();
                remove.Add(idx);
            }
        }
        if (remove is null)
        {
            return;
        }
        foreach (var idx in remove)
        {
            _byChar.Remove(idx);
        }
    }
}
