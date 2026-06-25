using System;
using System.Collections.Generic;
using System.IO;
using Argentum.Client.Resources;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Audio;

/// <summary>Reproduce SFX, ambiente y música AO (VB6 ao20audio + clsAudioEngine).</summary>
public partial class AoAudio : Node
{
    public enum FxCategory
    {
        General,
        Steps,
        Ambient,
    }

    private readonly Dictionary<int, AudioStream> _cache = new();
    private readonly Dictionary<string, AudioStreamPlayer> _labeled = new();
    private readonly List<AudioStreamPlayer> _pool = new();
    private string? _wavRoot;
    private string? _oggRoot;
    private string? _midiRoot;
    private int _poolCursor;
    private WorldSession? _listener;
    private AudioStreamPlayer? _ambientPlayer;
    private AudioStreamPlayer? _musicPlayer;

    public bool SfxEnabled { get; set; } = true;
    public bool StepsEnabled { get; set; } = true;
    public bool AmbientEnabled { get; set; } = true;
    public bool MusicEnabled { get; set; } = true;
    public float SfxVolumeDb { get; set; }
    public float StepsVolumeDb { get; set; }
    public float AmbientVolumeDb { get; set; } = -6f;
    public float MusicVolumeDb { get; set; } = -8f;

    public override void _Ready()
    {
        var root = ResourcesRoot.Resolve();
        if (root is not null)
        {
            _wavRoot = Path.Combine(root, "wav");
            _oggRoot = Path.Combine(root, "OGG");
            _midiRoot = Path.Combine(root, "MIDI");
        }
        for (var i = 0; i < 12; i++)
        {
            var player = new AudioStreamPlayer { Bus = "Master" };
            AddChild(player);
            _pool.Add(player);
        }
        _ambientPlayer = new AudioStreamPlayer { Bus = "Master" };
        AddChild(_ambientPlayer);
        _musicPlayer = new AudioStreamPlayer { Bus = "Master" };
        AddChild(_musicPlayer);
    }

    public void BindListener(WorldSession? session) => _listener = session;

    public void StopWorldAudio()
    {
        StopMusic();
        StopAmbient();
        BindListener(null);
    }

    public void PlayUi(int waveId) => PlayWaveDirect(waveId, SfxVolumeDb, 0f);

    public void PlayWave(
        int waveId,
        int srcX = 0,
        int srcY = 0,
        FxCategory category = FxCategory.General,
        string? label = null,
        bool loop = false)
    {
        if (!IsCategoryEnabled(category) || waveId <= 0)
        {
            return;
        }
        if (_listener is not null && srcX > 0 && srcY > 0
            && !SpatialAudio.IsInAudibleArea(srcX, srcY, _listener))
        {
            return;
        }
        var baseDb = CategoryBaseDb(category);
        var distance = _listener is null || srcX <= 0 || srcY <= 0
            ? 0
            : SpatialAudio.TileDistance(srcX, srcY, _listener.TileX, _listener.TileY);
        var volumeDb = srcX > 0 && srcY > 0
            ? SpatialAudio.VolumeDb(category, distance, baseDb)
            : baseDb;
        var pan = _listener is null || srcX <= 0
            ? 0f
            : SpatialAudio.Pan(srcX, _listener.TileX, distance);
        PlayWaveDirect(waveId, volumeDb, pan, label, loop);
    }

    public void PlayNetworkWave(int wave, int srcX, int srcY, int cancelLastWave, int localize, bool mapFogEnabled)
    {
        if (wave is >= 400 and <= 404 && !mapFogEnabled)
        {
            return;
        }
        if (cancelLastWave != 0)
        {
            StopWave(wave);
            if (cancelLastWave == 2)
            {
                return;
            }
        }
        var waveId = localize != 0 ? wave : wave;
        _ = localize;
        PlayWave(waveId, srcX, srcY);
    }

    public void PlayWaveDirect(int waveId, float volumeDb, float pan, string? label = null, bool loop = false)
    {
        if (waveId <= 0)
        {
            return;
        }
        var stream = ResolveStream(waveId);
        if (stream is null)
        {
            return;
        }
        var player = RentPlayer(label);
        player.Stream = stream;
        player.VolumeDb = volumeDb + pan * -4f;
        if (stream is AudioStreamWav wav)
        {
            wav.LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled;
        }
        else if (stream is AudioStreamOggVorbis ogg)
        {
            ogg.Loop = loop;
        }
        player.Play();
    }

    public void PlayAmbient(int waveId)
    {
        if (!AmbientEnabled || waveId <= 0 || _ambientPlayer is null)
        {
            return;
        }
        var stream = ResolveStream(waveId);
        if (stream is null)
        {
            return;
        }
        if (stream is AudioStreamOggVorbis ogg)
        {
            ogg.Loop = true;
        }
        else if (stream is AudioStreamWav wav)
        {
            wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        }
        _ambientPlayer.Stream = stream;
        _ambientPlayer.VolumeDb = AmbientVolumeDb;
        _ambientPlayer.Play();
    }

    public void StopAmbient()
    {
        _ambientPlayer?.Stop();
    }

    public void PlayMidi(int trackId, bool loop = true)
    {
        if (!MusicEnabled || trackId <= 0 || _musicPlayer is null)
        {
            return;
        }
        var stream = ResolveMusicStream(trackId);
        if (stream is null)
        {
            return;
        }
        if (stream is AudioStreamOggVorbis ogg)
        {
            ogg.Loop = loop;
        }
        _musicPlayer.Stream = stream;
        _musicPlayer.VolumeDb = MusicVolumeDb;
        _musicPlayer.Play();
    }

    public void StopMusic() => _musicPlayer?.Stop();

    public void StopWave(int waveId, string? label = null)
    {
        if (!string.IsNullOrEmpty(label) && _labeled.TryGetValue(label, out var labeled))
        {
            labeled.Stop();
            return;
        }
        var key = waveId.ToString();
        if (_labeled.TryGetValue(key, out var byId))
        {
            byId.Stop();
        }
    }

    private bool IsCategoryEnabled(FxCategory category) => category switch
    {
        FxCategory.Steps => SfxEnabled && StepsEnabled,
        FxCategory.Ambient => AmbientEnabled,
        _ => SfxEnabled,
    };

    private float CategoryBaseDb(FxCategory category) => category switch
    {
        FxCategory.Steps => StepsVolumeDb,
        FxCategory.Ambient => AmbientVolumeDb,
        _ => SfxVolumeDb,
    };

    private AudioStreamPlayer RentPlayer(string? label)
    {
        if (!string.IsNullOrEmpty(label) && _labeled.TryGetValue(label, out var existing))
        {
            return existing;
        }
        foreach (var player in _pool)
        {
            if (!player.Playing)
            {
                if (!string.IsNullOrEmpty(label))
                {
                    _labeled[label] = player;
                }
                return player;
            }
        }
        var rented = _pool[_poolCursor];
        _poolCursor = (_poolCursor + 1) % _pool.Count;
        rented.Stop();
        if (!string.IsNullOrEmpty(label))
        {
            _labeled[label] = rented;
        }
        return rented;
    }

    private AudioStream? ResolveMusicStream(int trackId)
    {
        if (_oggRoot is not null && Directory.Exists(_oggRoot))
        {
            foreach (var name in new[] { $"ost_{trackId}.ogg", $"{trackId}.ogg" })
            {
                var path = Path.Combine(_oggRoot, name);
                if (File.Exists(path))
                {
                    return AudioStreamOggVorbis.LoadFromFile(path);
                }
            }
        }
        if (_midiRoot is not null && Directory.Exists(_midiRoot))
        {
            var midiPath = Path.Combine(_midiRoot, $"{trackId}.mid");
            if (File.Exists(midiPath))
            {
                GD.Print($"[AoAudio] MIDI {trackId} encontrado pero Godot no reproduce .mid directamente.");
            }
        }
        return ResolveStream(trackId);
    }

    private AudioStream? ResolveStream(int waveId)
    {
        if (_cache.TryGetValue(waveId, out var cached))
        {
            return cached;
        }
        if (_wavRoot is null || !Directory.Exists(_wavRoot))
        {
            return null;
        }
        var id = waveId.ToString();
        var ogg = Path.Combine(_wavRoot, id + ".ogg");
        var wav = Path.Combine(_wavRoot, id + ".wav");
        string? path = File.Exists(ogg) ? ogg : File.Exists(wav) ? wav : null;
        if (path is null)
        {
            return null;
        }
        AudioStream? loaded = path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)
            ? AudioStreamOggVorbis.LoadFromFile(path)
            : AudioStreamWav.LoadFromFile(path);
        if (loaded is not null)
        {
            _cache[waveId] = loaded;
        }
        return loaded;
    }
}
