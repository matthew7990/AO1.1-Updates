using System;
using System.IO;

namespace Argentum.Client.World;

/// <summary>
/// Traza movimiento del jugador local. Activar con AO_MOVE_DEBUG=1.
/// Log: %TEMP%\ao-move.log (también imprime en consola Godot).
/// </summary>
internal static class MovementDiagnostics
{
    private static bool? _enabled;
    private static string? _logPath;

    public static bool Enabled =>
        _enabled ??= string.Equals(
            System.Environment.GetEnvironmentVariable("AO_MOVE_DEBUG"),
            "1",
            StringComparison.Ordinal);

    public static string LogPath =>
        _logPath ??= Path.Combine(Path.GetTempPath(), "ao-move.log");

    public static void ClearLog()
    {
        if (!Enabled)
        {
            return;
        }
        try
        {
            File.WriteAllText(LogPath, $"--- AO move diag {DateTime.Now:O}{System.Environment.NewLine}");
            Godot.GD.Print($"[MoveDiag] log en {LogPath}");
        }
        catch
        {
        }
    }

    public static void Log(string category, string message)
    {
        if (!Enabled)
        {
            return;
        }
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{category}] {message}";
        Godot.GD.Print($"[MoveDiag] [{category}] {message}");
        try
        {
            File.AppendAllText(LogPath, line + System.Environment.NewLine);
        }
        catch
        {
        }
    }

    public static void LogSession(WorldSession w, string category, string message)
    {
        Log(category, $"map={w.MapId} tile=({w.TileX},{w.TileY}) h={w.Heading} | {w.Motion.DescribeState()} | {message}");
    }

    public static string DescribeState(this CharacterMotion m) =>
        $"moving={m.IsMoving} camScroll={m.IsCameraScrolling} " +
        $"pCam=({m.PendingCameraX},{m.PendingCameraY}) " +
        $"camOff=({m.CameraOffsetX:F1},{m.CameraOffsetY:F1}) " +
        $"moveOff=({m.MoveOffsetX:F1},{m.MoveOffsetY:F1})";
}
