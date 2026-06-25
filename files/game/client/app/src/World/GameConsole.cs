using System;
using System.Collections.Generic;
using Godot;

namespace Argentum.Client.World;

/// <summary>VB6 frmMain.RecTxt — log de consola en juego.</summary>
public sealed class GameConsole
{
    public const int MaxLines = 100;

    public readonly struct Line
    {
        public string Text { get; init; }
        public Color Color { get; init; }
    }

    private readonly List<Line> _lines = new();

    public IReadOnlyList<Line> Lines => _lines;

    public void Add(string text, Color? color = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        _lines.Add(new Line
        {
            Text = text.Trim(),
            Color = color ?? new Color("c8c0b4"),
        });
        if (_lines.Count > MaxLines)
        {
            _lines.RemoveAt(0);
        }
    }

    public void Clear() => _lines.Clear();
}
