using System;

namespace Argentum.Client.World;

/// <summary>Overlay de debug del peek adyacente. Activar con AO_PEEK_DEBUG=1.</summary>
internal static class PeekDiagnostics
{
    private static bool? _enabled;
    private static bool? _gridEnabled;

    public static bool Enabled =>
        _enabled ??= string.Equals(
            Environment.GetEnvironmentVariable("AO_PEEK_DEBUG"),
            "1",
            StringComparison.Ordinal);

    /// <summary>Rectángulos por celda (costoso). Activar con AO_PEEK_DEBUG_GRID=1.</summary>
    public static bool GridEnabled =>
        _gridEnabled ??= string.Equals(
            Environment.GetEnvironmentVariable("AO_PEEK_DEBUG_GRID"),
            "1",
            StringComparison.Ordinal);
}
