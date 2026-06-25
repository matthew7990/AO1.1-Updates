using Argentum.Client.World;

namespace Argentum.Client.Audio;

/// <summary>VB6 TileEngine_Map.SwitchMap — música MIDI y ambiente del mapa.</summary>
public static class MapAudioService
{
    private static int _lastMusicId;
    private static int _lastAmbientId;

    public static void OnMapLoaded(CsmMap? map, AoAudio audio, bool nightTime = false)
    {
        if (map is null)
        {
            return;
        }
        OnMapLoadedIfChanged(map, audio, nightTime, force: true);
    }

    /// <summary>Transición a pie entre mapas: no reinicia MIDI/ambiente si no cambió.</summary>
    public static void OnMapLoadedIfChanged(CsmMap? map, AoAudio audio, bool nightTime = false, bool force = false)
    {
        if (map is null)
        {
            return;
        }
        var ambient = map.Audio.ResolveAmbient(nightTime);
        if (ambient != _lastAmbientId)
        {
            audio.StopAmbient();
            if (ambient > 0 && audio.AmbientEnabled)
            {
                audio.PlayAmbient(ambient);
            }
            _lastAmbientId = ambient;
        }
        else if (force && ambient > 0 && audio.AmbientEnabled)
        {
            audio.PlayAmbient(ambient);
            _lastAmbientId = ambient;
        }

        var music = map.Audio.MusicLow;
        if (music > 0 && audio.MusicEnabled && (force || music != _lastMusicId))
        {
            audio.PlayMidi(music, loop: true);
            _lastMusicId = music;
        }
    }
}
