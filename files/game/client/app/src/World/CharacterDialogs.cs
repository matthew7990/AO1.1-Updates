using System.Collections.Generic;
using Godot;

namespace Argentum.Client.World;

/// <summary>VB6 charlist().dialog + Char_Dialog_Set / Char_TextRender.</summary>
public sealed class CharacterDialogs
{
    public readonly struct Bubble
    {
        public string Speaker { get; init; }
        public string Text { get; init; }
        public Color Color { get; init; }
        public double StartMs { get; init; }
        public double LifeMs { get; init; }

        public float RiseOffset(double nowMs) => (float)((nowMs - StartMs) * 0.025);
        public float Alpha(double nowMs)
        {
            var elapsed = nowMs - StartMs;
            if (elapsed >= LifeMs)
            {
                return 0f;
            }
            if (elapsed > LifeMs - 400)
            {
                return (float)((LifeMs - elapsed) / 400.0);
            }
            return 1f;
        }
        public bool IsAlive(double nowMs) => nowMs - StartMs < LifeMs;
    }

    private readonly Dictionary<int, Bubble> _active = new();

    public void Set(int charIndex, string speaker, string text, Color color, double? lifeMs = null)
    {
        if (charIndex <= 0 || string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        var life = lifeMs ?? 3000 + text.Length * 50;
        _active[charIndex] = new Bubble
        {
            Speaker = speaker.Trim(),
            Text = text.Trim(),
            Color = color,
            StartMs = Time.GetTicksMsec(),
            LifeMs = life,
        };
    }

    public bool TryGet(int charIndex, out Bubble bubble)
    {
        bubble = default;
        if (!_active.TryGetValue(charIndex, out var entry))
        {
            return false;
        }
        var now = Time.GetTicksMsec();
        if (!entry.IsAlive(now))
        {
            _active.Remove(charIndex);
            return false;
        }
        bubble = entry;
        return true;
    }

    public void PruneExpired()
    {
        var now = Time.GetTicksMsec();
        List<int>? remove = null;
        foreach (var (idx, bubble) in _active)
        {
            if (!bubble.IsAlive(now))
            {
                remove ??= new List<int>();
                remove.Add(idx);
            }
        }
        if (remove is null)
        {
            return;
        }
        foreach (var idx in remove)
        {
            _active.Remove(idx);
        }
    }
}
