using Windrose.StateWeb.Core.Abstractions;
using Windrose.StateWeb.Core.Contracts;
using Windrose.StateWeb.Core.Models;

namespace Windrose.StateWeb.Core.Extensions;

public static class WindroseSurfaceExtensions
{
    public static WindroseHistoryExport ToHistoryExport(this IWindroseHistorySource source, DateTimeOffset generatedAt)
    {
        var entries = source.RecentHistory.OrderByDescending(entry => entry.Timestamp).ToArray();
        return new WindroseHistoryExport
        {
            GeneratedAt = generatedAt,
            EntryCount = entries.Length,
            Entries = entries
        };
    }

    public static WindroseOverlaySnapshot ToOverlaySnapshot(this IWindroseOverlaySnapshotSource source, DateTimeOffset generatedAt)
    {
        return new WindroseOverlaySnapshot
        {
            GeneratedAt = generatedAt,
            LogAvailable = source.LogAvailable,
            ParserStatus = source.ParserStatus,
            ServerName = source.ServerName,
            CurrentIslandId = source.CurrentIslandId,
            WorldName = source.WorldName,
            WorldPresetType = source.WorldPresetType,
            ConnectedPlayerCount = source.ConnectedPlayerCount,
            TotalPlayerCount = source.TotalPlayerCount,
            RecentEventCount = source.RecentEventCount,
            RecentHistoryCount = source.RecentHistoryCount,
            ObservedFamilyCount = source.ObservedFamilyCount,
            HasStandaloneShipDocument = source.HasStandaloneShipDocument,
            LatestBackupAge = source.LatestBackupAge,
            LatestBackupPath = source.LatestBackupPath,
            Highlights = source.Highlights
        };
    }

    public static WindroseTimeSeriesExport ToTimeSeriesExport(this IWindroseTimeSeriesSource source, DateTimeOffset generatedAt)
    {
        var points = new List<WindroseTimeSeriesPoint>();
        var history = source.History.OrderBy(entry => entry.Timestamp).ToArray();
        var connectedPlayers = source.ConnectedPlayerCount;
        var eventCount = source.EventCount;
        var historyCount = 0;
        var logAvailable = source.LogAvailable;

        foreach (var entry in history)
        {
            historyCount++;

            if (entry.Category.Equals("Event", StringComparison.OrdinalIgnoreCase))
            {
                eventCount++;
                connectedPlayers = entry.Type switch
                {
                    "PlayerJoined" => connectedPlayers + 1,
                    "PlayerDisconnected" or "PlayerLeft" => Math.Max(0, connectedPlayers - 1),
                    _ => connectedPlayers
                };
            }

            if (entry.Type == "LogAvailabilityChanged" &&
                entry.Properties.TryGetValue("available", out var availableText) &&
                bool.TryParse(availableText, out var available))
            {
                logAvailable = available;
            }

            points.Add(new WindroseTimeSeriesPoint
            {
                Timestamp = entry.Timestamp,
                LogAvailable = logAvailable,
                CurrentIslandId = entry.IslandId ?? source.CurrentIslandId,
                ConnectedPlayerCount = connectedPlayers,
                EventCount = eventCount,
                HistoryCount = historyCount
            });
        }

        if (points.Count == 0)
        {
            points.Add(new WindroseTimeSeriesPoint
            {
                Timestamp = generatedAt,
                LogAvailable = source.LogAvailable,
                CurrentIslandId = source.CurrentIslandId,
                ConnectedPlayerCount = source.ConnectedPlayerCount,
                EventCount = source.EventCount,
                HistoryCount = 0
            });
        }

        return new WindroseTimeSeriesExport
        {
            GeneratedAt = generatedAt,
            WindowStart = points.First().Timestamp,
            WindowEnd = points.Last().Timestamp,
            SampleCount = points.Count,
            Points = points
        };
    }

    public static WindroseTimeSeriesExport ToTimeSeriesExport(this WindroseTimeSeriesWindow window, DateTimeOffset generatedAt)
    {
        return ((IWindroseTimeSeriesSource)window).ToTimeSeriesExport(generatedAt);
    }
}
