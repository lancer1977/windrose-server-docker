using System.Text.Json;
using Windrose.StateWeb.Core.Abstractions;
using Windrose.StateWeb.Core.Contracts;
using Windrose.StateWeb.Core.Extensions;
using Windrose.StateWeb.Core.Models;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.State;

namespace Windrose.StateWeb.Api;

public static class WindroseEndpoints
{
    public static IEndpointRouteBuilder MapWindroseStateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", (IWindroseStateStore store) =>
        {
            var state = store.GetState();
            return Results.Ok(new
            {
                status = state.LogAvailable ? "ok" : "degraded",
                state.LogAvailable,
                state.LastLogRead,
                state.ParserStatus,
                state.ParserError,
                saveError = state.Save.Error
            });
        });

        endpoints.MapGet("/api/state", (IWindroseStateStore store) => store.GetState());
        endpoints.MapGet("/api/players", (IWindroseStateStore store) => store.GetState().Players);
        endpoints.MapGet("/api/events", (IWindroseStateStore store) => store.GetState().RecentEvents);
        endpoints.MapGet("/snapshot", (IWindroseStateStore store) => BuildOverlaySnapshot(store.GetState()));
        endpoints.MapGet("/eventsrecent", (IWindroseStateStore store) => store.GetState().RecentHistory);
        endpoints.MapGet("/events/recent", (IWindroseStateStore store) => store.GetState().RecentHistory);
        endpoints.MapGet("/api/history", (IWindroseStateStore store) => store.GetState().RecentHistory);
        endpoints.MapGet("/api/saves/latest", (IWindroseStateStore store) => store.GetState().Save);
        endpoints.MapGet("/api/saves/latest/checkpoint", (IWindroseStateStore store) =>
        {
            var save = store.GetState().Save;
            return Results.Ok(new
            {
                save.CheckpointContainerFormat,
                save.CheckpointExtractedPath,
                save.CheckpointEntries,
                readOnly = true
            });
        });
        endpoints.MapGet("/api/saves/latest/observed-families", (IWindroseStateStore store) =>
        {
            var save = store.GetState().Save;
            return Results.Ok(new
            {
                readOnly = true,
                hasStandaloneShipDocument = false,
                observedFamilies = save.ObservedFamilies
            });
        });
        endpoints.MapGet("/api/server/description", (IWindroseStateStore store) =>
        {
            var description = store.GetState().ServerDescription ?? store.GetState().Save.ServerDescription;
            return description is null
                ? Results.NotFound()
                : Results.Ok(description);
        });
        endpoints.MapGet("/api/world/description", (IWindroseStateStore store) =>
        {
            var save = store.GetState().Save;
            return Results.Ok(new
            {
                save.ActiveIslandId,
                save.WorldIslandId,
                save.WorldName,
                save.WorldPresetType,
                save.WorldSettingCount,
                save.WorldBoolSettingCount,
                save.WorldFloatSettingCount,
                save.WorldTagSettingCount,
                save.LatestBackupPath,
                save.LatestBackupTime,
                save.ServerDescription
            });
        });
        endpoints.MapGet("/api/world/entities", (IWindroseStateStore store) =>
        {
            var save = store.GetState().Save;
            return Results.Ok(BuildWorldSlice(save, ["island", "actor", "player-in-world-metadata", "ship-reference"], false));
        });
        endpoints.MapGet("/api/world/players", (IWindroseStateStore store) =>
        {
            var save = store.GetState().Save;
            return Results.Ok(BuildWorldSlice(save, ["player-in-world-metadata"], false));
        });
        endpoints.MapGet("/api/world/ships", (IWindroseStateStore store) =>
        {
            var save = store.GetState().Save;
            return Results.Ok(BuildWorldSlice(save, ["ship-reference", "ship-document"], false));
        });
        endpoints.MapGet("/api/world/actors", (IWindroseStateStore store) =>
        {
            var save = store.GetState().Save;
            return Results.Ok(BuildWorldSlice(save, ["actor"], false));
        });
        endpoints.MapGet("/api/world/summary", (IWindroseStateStore store) =>
        {
            var state = store.GetState();
            var save = state.Save;
            return Results.Ok(new
            {
                readOnly = true,
                hasDecodedDocuments = false,
                state.CurrentIslandId,
                save.WorldIslandId,
                save.WorldName,
                save.WorldPresetType,
                observedFamilyCount = save.ObservedFamilies.Count,
                observedFamilies = save.ObservedFamilies.Select(family => new
                {
                    family.Name,
                    family.Status
                }),
                hasStandaloneShipDocument = save.ObservedFamilies.Any(family => family.Name == "ship-document" && family.Status == "present")
            });
        });
        endpoints.MapGet("/api/history/export", (IWindroseStateStore store) =>
        {
            var state = store.GetState();
            return Results.Ok(state.ToHistoryExport(DateTimeOffset.UtcNow));
        });
        endpoints.MapGet("/api/history/timeseries", (IWindroseStateStore store) =>
        {
            var state = store.GetState();
            return Results.Ok(BuildTimeSeriesExport(state));
        });
        endpoints.MapGet("/api/overlay/summary", (IWindroseStateStore store) =>
        {
            return Results.Ok(BuildOverlaySnapshot(store.GetState()));
        });

        endpoints.MapGet("/api/events/stream", async (HttpContext context, IWindroseStateStore store) =>
        {
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Content-Type", "text/event-stream");

            var reader = store.Subscribe(context.RequestAborted);
            await foreach (var evt in reader.ReadAllAsync(context.RequestAborted))
            {
                await context.Response.WriteAsync($"event: {evt.Type}\n", context.RequestAborted);
                await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(evt)}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        });

        return endpoints;
    }

    private static object BuildWorldSlice(Windrose.StateWeb.Domain.SaveMetadata save, IReadOnlyCollection<string> familyNames, bool hasDecodedDocuments)
    {
        var observedFamilies = save.ObservedFamilies.Where(family => familyNames.Contains(family.Name, StringComparer.OrdinalIgnoreCase)).ToArray();

        return new
        {
            readOnly = true,
            hasDecodedDocuments,
            observedFamilies
        };
    }

    private static string? FormatAge(TimeSpan? age)
    {
        if (age is null)
        {
            return null;
        }

        if (age.Value.TotalMinutes < 1)
        {
            return $"{age.Value.Seconds}s ago";
        }

        if (age.Value.TotalHours < 1)
        {
            return $"{(int)age.Value.TotalMinutes}m ago";
        }

        return $"{(int)age.Value.TotalHours}h {age.Value.Minutes}m ago";
    }

    private static WindroseOverlaySnapshot BuildOverlaySnapshot(WindroseServerState state)
    {
        var save = state.Save;

        var context = new WindroseOverlaySnapshotContext
        {
            LogAvailable = state.LogAvailable,
            ParserStatus = state.ParserStatus,
            ServerName = state.ServerName ?? save.ServerDescription?.ServerName,
            CurrentIslandId = state.CurrentIslandId ?? save.WorldIslandId,
            WorldName = save.WorldName,
            WorldPresetType = save.WorldPresetType,
            ConnectedPlayerCount = state.Players.Count(player => player.IsConnected),
            TotalPlayerCount = state.Players.Count,
            RecentEventCount = state.RecentEvents.Count,
            RecentHistoryCount = state.RecentHistory.Count,
            ObservedFamilyCount = save.ObservedFamilies.Count,
            HasStandaloneShipDocument = save.ObservedFamilies.Any(family => family.Name == "ship-document" && family.Status == "present"),
            LatestBackupAge = FormatAge(save.BackupAge),
            LatestBackupPath = save.LatestBackupPath,
            Highlights =
            [
                state.LogAvailable ? "log:available" : "log:missing",
                state.IsReady ? "server:ready" : "server:starting",
                save.ObservedFamilies.Any(family => family.Name == "ship-document" && family.Status == "present") ? "ship:present" : "ship:summary-only",
                "consumer:channel-cheevos / cc-sidecar"
            ]
        };

        return context.ToOverlaySnapshot(DateTimeOffset.UtcNow);
    }

    private static WindroseTimeSeriesExport BuildTimeSeriesExport(WindroseServerState state)
    {
        var window = new WindroseTimeSeriesWindow
        {
            History = state.RecentHistory,
            LogAvailable = state.LogAvailable,
            CurrentIslandId = state.CurrentIslandId,
            ConnectedPlayerCount = state.Players.Count(player => player.IsConnected),
            EventCount = state.RecentEvents.Count
        };

        return window.ToTimeSeriesExport(DateTimeOffset.UtcNow);
    }
}
