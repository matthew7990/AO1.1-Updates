using Argentum.Client.Audio;
using Argentum.Client.Protocol;
using Argentum.Client.Resources;
using Argentum.Client.World;

namespace Argentum.Client.Network;

/// <summary>VB6 HandlePlayWaveStep — pasos de personajes invisibles (ePlayWaveStep 188).</summary>
public static class PlayWaveStepReader
{
    public static void Apply(
        ref LegacyPacketReader reader,
        AoAudio? audio,
        WorldSession session,
        GrhCatalog? grhs)
    {
        _ = reader.ReadInt16(); // charIndex
        var grh1 = reader.ReadInt32();
        var grh2 = reader.ReadInt32();
        var distance = reader.ReadUInt8();
        var balance = reader.ReadInt16();
        var step = reader.ReadBoolean();
        if (audio is null)
        {
            return;
        }
        FootstepAudio.PlayFromNetwork(audio, session, grhs, grh1, grh2, distance, balance, step);
    }
}
