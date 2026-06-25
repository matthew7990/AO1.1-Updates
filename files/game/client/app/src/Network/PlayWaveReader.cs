using Argentum.Client.Audio;
using Argentum.Client.Protocol;
using Argentum.Client.World;

namespace Argentum.Client.Network;

public static class PlayWaveReader
{
    public static void Apply(ref LegacyPacketReader reader, AoAudio? audio, WorldSession? session)
    {
        var wave = reader.ReadInt16();
        var srcX = reader.ReadUInt8();
        var srcY = reader.ReadUInt8();
        var cancelLastWave = reader.ReadUInt8();
        var localize = reader.ReadUInt8();
        NetDiagnostics.Log("PlayWave", $"id={wave} pos=({srcX},{srcY}) cancel={cancelLastWave}");
        if (audio is null)
        {
            return;
        }
        var fog = session?.Map?.Audio.Fog ?? false;
        audio.PlayNetworkWave(wave, srcX, srcY, cancelLastWave, localize, fog);
    }
}
