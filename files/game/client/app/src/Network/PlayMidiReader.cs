using Argentum.Client.Audio;
using Argentum.Client.Protocol;

namespace Argentum.Client.Network;

/// <summary>VB6 HandlePlayMIDI — ePlayMIDI (54).</summary>
public static class PlayMidiReader
{
    public static void Apply(ref LegacyPacketReader reader, AoAudio? audio)
    {
        var track = reader.ReadUInt8();
        var loop = reader.ReadInt16() != 0;
        audio?.PlayMidi(track, loop);
    }
}
