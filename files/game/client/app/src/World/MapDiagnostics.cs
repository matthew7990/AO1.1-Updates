using System;
using System.IO;
using Godot;

namespace Argentum.Client.World;

internal static class MapDiagnostics
{
    private static bool? _enabled;
    private static string? _logPath;

    public static bool Enabled =>
        _enabled ??= string.Equals(
            System.Environment.GetEnvironmentVariable("AO_MAP_DEBUG"),
            "1",
            StringComparison.Ordinal);

    public static string LogPath =>
        _logPath ??= Path.Combine(Path.GetTempPath(), "ao-mapdiag.log");

    public static void Log(string message)
    {
        if (!Enabled)
        {
            return;
        }
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        GD.Print($"[MapDiag] {message}");
        try
        {
            File.AppendAllText(LogPath, line + System.Environment.NewLine);
        }
        catch
        {
        }
    }

    public static void ClearLog()
    {
        if (!Enabled)
        {
            return;
        }
        try
        {
            File.WriteAllText(LogPath, $"--- AO map diag {DateTime.Now:O}{System.Environment.NewLine}");
        }
        catch
        {
        }
    }
}
