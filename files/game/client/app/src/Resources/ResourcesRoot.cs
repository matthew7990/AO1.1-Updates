using System;
using System.IO;

namespace Argentum.Client.Resources;

internal static class ResourcesRoot
{
    public static string? Resolve()
    {
        var env = Environment.GetEnvironmentVariable("AO_RESOURCES");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
        {
            return Path.GetFullPath(env);
        }
        var exeDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var cwd = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(exeDir, "data", "resources")),
            Path.GetFullPath(Path.Combine(exeDir, "..", "data", "resources")),
            Path.GetFullPath(Path.Combine(cwd, "..", "reference", "resources")),
            Path.GetFullPath(Path.Combine(cwd, "reference", "resources")),
            Path.GetFullPath(Path.Combine(cwd, "..", "..", "AO", "reference", "resources")),
        };
        foreach (var candidate in candidates)
        {
            if (Directory.Exists(Path.Combine(candidate, "init", "Graficos.ini")) ||
                Directory.Exists(Path.Combine(candidate, "init")))
            {
                return candidate;
            }
        }
        return null;
    }
}
