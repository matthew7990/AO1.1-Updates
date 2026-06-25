using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Argentum.Client.Network;

/// <summary>Host:puerto local — AO_SERVER, tools/ao-local.json + Server.ini.</summary>
internal static class AoEndpoint
{
    private static readonly Regex IniValue = new(@"^\s*(?<key>[^=]+?)\s*=\s*(?<value>.+?)\s*$", RegexOptions.Compiled);

    public static string Resolve()
    {
        var env = Environment.GetEnvironmentVariable("AO_SERVER");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env.Trim();
        }

        var toolsRoot = FindToolsDirectory();
        if (toolsRoot is null)
        {
            return "127.0.0.1:7667";
        }

        var localJson = Path.Combine(toolsRoot, "ao-local.json");
        var host = ReadJsonHost(localJson);
        var port = ReadJsonPort(localJson);
        if (port <= 0)
        {
            var iniPath = Path.Combine(Directory.GetParent(toolsRoot)!.FullName, "reference", "server-vb6", "Server.ini");
            port = ReadIniPort(iniPath) ?? 7667;
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            host = DetectLanIp() ?? "127.0.0.1";
        }

        return $"{host.Trim()}:{port}";
    }

    private static string? FindToolsDirectory()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = start;
            for (var i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
            {
                var tools = Path.Combine(dir, "tools");
                if (File.Exists(Path.Combine(tools, "ao-local.json")))
                {
                    return tools;
                }
                dir = Directory.GetParent(dir)?.FullName ?? "";
            }
        }
        return null;
    }

    private static string? ReadJsonHost(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("clientHost", out var h) ? h.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static int ReadJsonPort(string path)
    {
        if (!File.Exists(path))
        {
            return 0;
        }
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("serverPort", out var p) && p.TryGetInt32(out var v))
            {
                return v;
            }
        }
        catch { /* ignore */ }
        return 0;
    }

    private static int? ReadIniPort(string iniPath)
    {
        if (!File.Exists(iniPath))
        {
            return null;
        }
        foreach (var line in File.ReadAllLines(iniPath))
        {
            var m = IniValue.Match(line);
            if (m.Success && m.Groups["key"].Value.Equals("StartPort", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(m.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
            {
                return port;
            }
        }
        return null;
    }

    private static string? DetectLanIp()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up
                    || nic.NetworkInterfaceType is NetworkInterfaceType.Loopback
                        or NetworkInterfaceType.Tunnel)
                {
                    continue;
                }
                foreach (var addr in nic.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }
                    var ip = addr.Address.ToString();
                    if (!ip.StartsWith("127.", StringComparison.Ordinal) && !ip.StartsWith("169.254.", StringComparison.Ordinal))
                    {
                        return ip;
                    }
                }
            }
        }
        catch { /* ignore */ }

        try
        {
            foreach (var addr in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr))
                {
                    return addr.ToString();
                }
            }
        }
        catch { /* ignore */ }

        return null;
    }
}
