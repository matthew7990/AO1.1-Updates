using Argentum.Client.Protocol;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Network;

/// <summary>VB6 HandleTextOverChar — eTextOverChar (125), daño flotante sobre personajes.</summary>
public static class TextOverCharReader
{
    public static void Apply(ref LegacyPacketReader reader, WorldSession session)
    {
        var text = reader.ReadString8();
        var charIndex = reader.ReadInt16();
        var colorRaw = reader.ReadInt32();
        if (charIndex <= 0 || string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        session.FloatingTexts.Add(charIndex, text, VbColorToGodot(colorRaw));
    }

    private static Color VbColorToGodot(int vbColor)
    {
        if (vbColor == 0 || vbColor == -1)
        {
            return Colors.White;
        }
        var b = (vbColor >> 16) & 0xFF;
        var g = (vbColor >> 8) & 0xFF;
        var r = vbColor & 0xFF;
        return new Color(r / 255f, g / 255f, b / 255f);
    }
}
