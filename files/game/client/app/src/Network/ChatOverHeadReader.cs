using Argentum.Client.Protocol;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Network;

/// <summary>VB6 Protocol.HandleChatOverHead + WriteChatOverHeadInConsole.</summary>
public static class ChatOverHeadReader
{
    public readonly struct Data
    {
        public string Chat { get; init; }
        public int CharIndex { get; init; }
        public int ColorRaw { get; init; }
    }

    public static Data Parse(ref LegacyPacketReader reader) => new()
    {
        Chat = reader.ReadString8(),
        CharIndex = reader.ReadInt16(),
        ColorRaw = reader.ReadInt32(),
        // esSpell + pos + display times — VB6 wire layout
    };

    public static void SkipTail(ref LegacyPacketReader reader)
    {
        _ = reader.ReadBoolean();
        _ = reader.ReadUInt8();
        _ = reader.ReadUInt8();
        _ = reader.ReadInt16();
        _ = reader.ReadInt16();
    }

    public static Data ParseFull(ref LegacyPacketReader reader)
    {
        var data = Parse(ref reader);
        SkipTail(ref reader);
        return data;
    }

    public static void Apply(in Data data, WorldSession session, WorldCharacters characters, bool skipOwnConsoleEcho = true)
    {
        if (string.IsNullOrWhiteSpace(data.Chat))
        {
            return;
        }

        var textColor = VbColorToGodot(data.ColorRaw);
        var speaker = ResolveSpeaker(data.CharIndex, session, characters);
        session.Dialogs.Set(data.CharIndex, speaker, data.Chat, textColor);

        if (skipOwnConsoleEcho && data.CharIndex == session.CharIndex)
        {
            return;
        }

        if (string.IsNullOrEmpty(speaker))
        {
            session.Console.Add(data.Chat, textColor);
            return;
        }

        session.Console.Add($"[{speaker}] {data.Chat}", SpeakerConsoleColor(data.CharIndex, session));
    }

    private static string ResolveSpeaker(int charIndex, WorldSession session, WorldCharacters characters)
    {
        if (charIndex == session.CharIndex)
        {
            return session.CharacterName;
        }
        if (characters.TryGet((short)charIndex, out var ch) && !string.IsNullOrWhiteSpace(ch!.Name))
        {
            return ch.Name;
        }
        return "";
    }

    private static Color SpeakerConsoleColor(int charIndex, WorldSession session) =>
        charIndex == session.CharIndex ? new Color("7ab8ff") : new Color("e8c878");

    private static Color VbColorToGodot(int vbColor)
    {
        if (vbColor == 0 || vbColor == -1)
        {
            return new Color("f0ece4");
        }
        var b = (vbColor >> 16) & 0xFF;
        var g = (vbColor >> 8) & 0xFF;
        var r = vbColor & 0xFF;
        return new Color(r / 255f, g / 255f, b / 255f);
    }
}
