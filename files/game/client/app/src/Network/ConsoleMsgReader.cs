using Argentum.Client.Protocol;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Network;

/// <summary>VB6 Protocol.HandleConsoleMessage (subset — texto + font index).</summary>
public static class ConsoleMsgReader
{
    public static void Apply(ref LegacyPacketReader reader, WorldSession session)
    {
        var chat = reader.ReadString8();
        var fontIndex = reader.ReadUInt8();
        var color = FontIndexToColor(fontIndex);
        session.Console.Add(chat, color);
        session.GameMessage = chat;
    }

    private static Color FontIndexToColor(int fontIndex) => fontIndex switch
    {
        1 or 7 => new Color("e8c040"),  // warning / talk
        2 or 8 => new Color("e04a4a"),  // fight / error
        3 => new Color("5ec8ff"),       // guild
        4 => new Color("7ab8ff"),       // party
        5 => new Color("9ad45a"),       // faction
        _ => new Color("c8c0b4"),
    };
}
