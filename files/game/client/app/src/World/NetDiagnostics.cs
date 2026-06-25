using System;
using System.IO;

namespace Argentum.Client.World;

/// <summary>Traza paquetes de combate/muerte — siempre activo en %TEMP%\ao-net.log.</summary>
internal static class NetDiagnostics
{
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "ao-net.log");

    public static void Log(string category, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{category}] {message}";
        Godot.GD.Print($"[AONet] [{category}] {message}");
        try
        {
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        catch
        {
        }
    }
}
