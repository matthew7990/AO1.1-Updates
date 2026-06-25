using System;
using System.Collections.Generic;
using Godot;

namespace Argentum.Client.Core;

/// <summary>
/// Resolución fija por preset. El mundo renderiza en SubViewport a preset.Width×Height (1:1, sin stretch).
/// </summary>
public static class GameViewport
{
    public const int MinWidth = 640;
    public const int MinHeight = 480;
    public const int TilePixels = 32;

    private const string SettingsPath = "user://ao_display.cfg";

    private static readonly RenderPreset[] AllPresets =
    [
        RenderPreset.Hd720,
        RenderPreset.Hd1080,
        RenderPreset.Qhd1440,
    ];

    private static RenderPreset? _active;
    private static SubViewport? _worldViewport;
    private static SubViewportContainer? _worldHost;
    private static bool _windowed;

    public static RenderPreset Active
    {
        get
        {
            if (_active is { } preset)
            {
                return preset;
            }
            var env = System.Environment.GetEnvironmentVariable("AO_RESOLUTION");
            _active = string.IsNullOrWhiteSpace(env) ? RenderPreset.Hd1080 : RenderPreset.Parse(env);
            return _active.Value;
        }
    }

    public static IReadOnlyList<RenderPreset> AvailablePresets => AllPresets;

    public static bool Windowed => _windowed;

    public static int GetPresetIndex(RenderPreset preset)
    {
        for (var i = 0; i < AllPresets.Length; i++)
        {
            if (AllPresets[i].Name == preset.Name)
            {
                return i;
            }
        }
        return 1;
    }

    public static void SetPreset(RenderPreset preset, Window window)
    {
        _active = preset;
        ApplyWindowMode(window, preset);
        ScheduleRestoreLayout(window);
        SaveUserSettings();
    }

    public static void SetWindowed(bool windowed, Window window)
    {
        _windowed = windowed;
        ApplyWindowMode(window, Active);
        ScheduleRestoreLayout(window);
        SaveUserSettings();
    }

    public static Vector2 LogicalSize
    {
        get
        {
            var preset = Active;
            return new Vector2(preset.Width, preset.Height);
        }
    }

    public static void Configure(Window window, SubViewport worldViewport, SubViewportContainer worldHost)
    {
        _worldViewport = worldViewport;
        _worldHost = worldHost;
        _worldHost.Stretch = false;
        _windowed = System.Environment.GetEnvironmentVariable("AO_WINDOWED") == "1";
        TryLoadUserSettings();

        var preset = Active;
        window.ContentScaleMode = Window.ContentScaleModeEnum.Disabled;
        ApplyWindowMode(window, preset);
        ScheduleRestoreLayout(window);

        window.FocusEntered += () => ScheduleRestoreLayout(window);
        GD.Print($"AO render preset: {preset.Name} ({preset.HalfTilesX * 2}×{preset.HalfTilesY * 2} tiles, {preset.Width}×{preset.Height}px)");
    }

    /// <summary>Re-centra el mundo tras cambio de modo/resolución (inmediato + frame siguiente).</summary>
    public static void ScheduleRestoreLayout(Window window)
    {
        RestoreLayout(window);
        Callable.From(() => RestoreLayout(window)).CallDeferred();
    }

    public static void RestoreLayout(Window window)
    {
        ApplyPresetSize(Active);
        LayoutWorldHost(window);
    }

    private static void ApplyPresetSize(RenderPreset preset)
    {
        if (_worldViewport is not null)
        {
            _worldViewport.Size = new Vector2I(preset.Width, preset.Height);
            _worldViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        }
        if (_worldHost is not null)
        {
            _worldHost.Stretch = false;
            _worldHost.Size = new Vector2(preset.Width, preset.Height);
        }
    }

    private static void LayoutWorldHost(Window window)
    {
        if (_worldHost is null)
        {
            return;
        }
        var preset = Active;
        var viewSize = window.GetViewport().GetVisibleRect().Size;
        if (viewSize.X < 1f || viewSize.Y < 1f)
        {
            viewSize = new Vector2(preset.Width, preset.Height);
        }
        _worldHost.Stretch = false;
        _worldHost.Size = new Vector2(preset.Width, preset.Height);
        _worldHost.Position = new Vector2(
            MathF.Floor((viewSize.X - preset.Width) * 0.5f),
            MathF.Floor((viewSize.Y - preset.Height) * 0.5f));
    }

    public static Vector2 GetRenderSize(Viewport viewport) => LogicalSize;

    public static bool TryMapGlobalMouseToLogical(SubViewportContainer host, Vector2 globalMouse, out Vector2 logicalPoint)
    {
        logicalPoint = default;
        var rect = host.GetGlobalRect();
        if (!rect.HasPoint(globalMouse))
        {
            return false;
        }
        logicalPoint = globalMouse - rect.Position;
        return logicalPoint.X >= 0 && logicalPoint.Y >= 0
               && logicalPoint.X <= rect.Size.X && logicalPoint.Y <= rect.Size.Y;
    }

    public static Vector2 MapLogicalPointToGlobal(SubViewportContainer host, Vector2 logicalPoint) =>
        host.GetGlobalRect().Position + logicalPoint;

    private static void ApplyWindowMode(Window window, RenderPreset preset)
    {
        if (_windowed)
        {
            window.Mode = Window.ModeEnum.Windowed;
            window.Unresizable = true;
            window.Size = new Vector2I(preset.Width, preset.Height);
        }
        else
        {
            window.Mode = Window.ModeEnum.Fullscreen;
        }
    }

    private static void TryLoadUserSettings()
    {
        if (!string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("AO_RESOLUTION")))
        {
            return;
        }
        if (!string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("AO_WINDOWED")))
        {
            return;
        }
        var cfg = new ConfigFile();
        if (cfg.Load(SettingsPath) != Error.Ok)
        {
            return;
        }
        if (cfg.GetValue("display", "resolution").AsString() is { Length: > 0 } resolution)
        {
            _active = RenderPreset.Parse(resolution);
        }
        if (cfg.HasSectionKey("display", "windowed"))
        {
            _windowed = cfg.GetValue("display", "windowed").AsBool();
        }
    }

    private static void SaveUserSettings()
    {
        if (!string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("AO_RESOLUTION"))
            || !string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("AO_WINDOWED")))
        {
            return;
        }
        var cfg = new ConfigFile();
        cfg.SetValue("display", "resolution", Active.Name);
        cfg.SetValue("display", "windowed", _windowed);
        cfg.Save(SettingsPath);
    }
}
