using System.Text.RegularExpressions;
using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Parsing;

public interface IWindroseLogParser
{
    WindroseEvent? ParseLine(string line);
}

public sealed partial class WindroseLogParser : IWindroseLogParser
{
    public WindroseEvent? ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var timestamp = ParseTimestamp(line) ?? DateTimeOffset.UtcNow;

        if (line.Contains("Server initialized. CurrentIslandId", StringComparison.Ordinal))
        {
            var islandId = MatchValue(line, @"CurrentIslandId\s+([A-F0-9]{16,})");
            return new WindroseEvent(timestamp, "ServerInitialized", "Information", "Server initialized", Properties: Props(("islandId", islandId)));
        }

        if (line.Contains("Host server is ready for owner to connect", StringComparison.Ordinal))
        {
            return new WindroseEvent(timestamp, "ServerReady", "Information", "Host server is ready for owner to connect");
        }

        if (line.Contains("Register server.", StringComparison.Ordinal) ||
            line.Contains("\"InviteCode\"", StringComparison.Ordinal) ||
            line.Contains("\"ServerName\"", StringComparison.Ordinal) ||
            line.Contains("\"MaxPlayerCount\"", StringComparison.Ordinal) ||
            line.Contains("\"UseDirectConnection\"", StringComparison.Ordinal) ||
            line.Contains("\"DirectConnectionServerPort\"", StringComparison.Ordinal) ||
            line.Contains("\"WorldIslandId\"", StringComparison.Ordinal))
        {
            var property = ParseSettingsLine(line);
            if (property is not null)
            {
                return new WindroseEvent(timestamp, "ServerSettingsObserved", "Information", $"{property.Value.Key} observed", Properties: Props((property.Value.Key, property.Value.Value)));
            }
        }

        if (line.Contains("Process AddPlayer", StringComparison.Ordinal) ||
            line.Contains("Reserve slot for Coop account", StringComparison.Ordinal))
        {
            return PlayerEvent(timestamp, "PlayerReserved", "Player session observed", line);
        }

        if (line.Contains("OnBLConnect", StringComparison.Ordinal) ||
            line.Contains("OnAccountBLConnected", StringComparison.Ordinal))
        {
            return PlayerEvent(timestamp, "PlayerBlConnected", "Player business-logic connection established", line);
        }

        if (line.Contains("OnUeConnect", StringComparison.Ordinal) ||
            line.Contains("Accept connection request", StringComparison.Ordinal))
        {
            return PlayerEvent(timestamp, "PlayerUeConnected", "Player Unreal connection established", line);
        }

        if (line.Contains("Login request:", StringComparison.Ordinal))
        {
            return PlayerEvent(timestamp, "PlayerLoginRequested", "Player login requested", line);
        }

        if (line.Contains("Join request:", StringComparison.Ordinal))
        {
            return PlayerEvent(timestamp, "PlayerJoined", "Player joined", line);
        }

        if (line.Contains("OnAccountDisconnected", StringComparison.Ordinal) ||
            line.Contains("OnAccountBLDisconnected", StringComparison.Ordinal) ||
            line.Contains("OnAccountUeDisconnected", StringComparison.Ordinal) ||
            line.Contains("UNetConnection::Close", StringComparison.Ordinal) ||
            line.Contains("Connection closed for", StringComparison.Ordinal))
        {
            return PlayerEvent(timestamp, "PlayerDisconnected", "Player disconnected", line, "Warning");
        }

        if (line.Contains("Save backup requested", StringComparison.Ordinal))
        {
            return new WindroseEvent(timestamp, "SaveBackupRequested", "Information", "Save backup requested", Properties: Props(("islandId", MatchValue(line, @"R5BLIsland\[([A-F0-9]+)\]"))));
        }

        if (line.Contains("Save backup has finished successfully", StringComparison.Ordinal))
        {
            return new WindroseEvent(timestamp, "SaveBackupFinished", "Information", "Save backup finished successfully", Properties: Props(("islandId", MatchValue(line, @"R5BLIsland\[([A-F0-9]+)\]"))));
        }

        if (line.Contains("Resource Usage History", StringComparison.Ordinal))
        {
            return new WindroseEvent(timestamp, "ResourceUsageObserved", "Information", "Resource usage report observed");
        }

        if (line.Contains(": Warning:", StringComparison.Ordinal))
        {
            return new WindroseEvent(timestamp, "WarningObserved", "Warning", TrimLine(line));
        }

        if (line.Contains(": Error:", StringComparison.Ordinal))
        {
            return new WindroseEvent(timestamp, "ErrorObserved", "Error", TrimLine(line));
        }

        return null;
    }

    private static WindroseEvent PlayerEvent(DateTimeOffset timestamp, string type, string message, string line, string severity = "Information")
    {
        var sessionId = MatchValue(line, @"BLPlayerSessionId[=\s']+([a-fA-F0-9]{16,})")
            ?? MatchValue(line, @"R5:([a-fA-F0-9]{16,})")
            ?? MatchValue(line, @"\(([a-fA-F0-9]{16,})\)");
        var accountId = MatchValue(line, @"AccountId[=\s']+([A-F0-9]{16,})");
        var clientName = MatchValue(line, @"[?&]Name=([^?\s]+)")
            ?? MatchValue(line, @"UniqueId:\s+NULL:([^\s,]+)");
        var disconnectReason = MatchValue(line, @"DisconnectReason '([^']+)'")
            ?? MatchValue(line, @"FarewellReason '([^']+)'")
            ?? MatchValue(line, @"Status message:? ([^.\r\n]+)");

        return new WindroseEvent(
            timestamp,
            type,
            severity,
            message,
            sessionId,
            accountId,
            Uri.UnescapeDataString(clientName ?? ""),
            Props(("disconnectReason", disconnectReason), ("raw", TrimLine(line))));
    }

    private static DateTimeOffset? ParseTimestamp(string line)
    {
        var match = TimestampRegex().Match(line);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups["value"].Value.Replace('.', '-');
        return DateTimeOffset.TryParseExact(value, "yyyy-MM-dd-HH:mm:ss:fff", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }

    private static KeyValuePair<string, string?>? ParseSettingsLine(string line)
    {
        var match = JsonSettingRegex().Match(line);
        if (!match.Success)
        {
            return null;
        }

        return new KeyValuePair<string, string?>(match.Groups["key"].Value, match.Groups["value"].Value.Trim('"'));
    }

    private static string? MatchValue(string line, string pattern)
    {
        var match = Regex.Match(line, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static IReadOnlyDictionary<string, string>? Props(params (string Key, string? Value)[] values)
    {
        var props = values
            .Where(v => !string.IsNullOrWhiteSpace(v.Value))
            .ToDictionary(v => v.Key, v => v.Value!);
        return props.Count == 0 ? null : props;
    }

    private static string TrimLine(string line) => line.Length <= 300 ? line : line[..300];

    [GeneratedRegex(@"\[(?<value>\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}:\d{3})\]")]
    private static partial Regex TimestampRegex();

    [GeneratedRegex("""["'](?<key>InviteCode|ServerName|WorldIslandId|MaxPlayerCount|UseDirectConnection|DirectConnectionServerPort)["']\s*:\s*(?<value>[^,\r\n]+)""")]
    private static partial Regex JsonSettingRegex();
}
