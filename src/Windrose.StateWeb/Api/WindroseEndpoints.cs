using System.Text.Json;
using Microsoft.Extensions.Options;
using Windrose.StateWeb.Core.Abstractions;
using Windrose.StateWeb.Core.Contracts;
using Windrose.StateWeb.Core.Extensions;
using Windrose.StateWeb.Core.Models;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.Options;
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

        endpoints.MapGet("/api/state", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var state = MaybeRedact(store.GetState(), options.Value.RedactSensitiveMetadata);
            return Results.Ok(state);
        });
        endpoints.MapGet("/api/players", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var state = MaybeRedact(store.GetState(), options.Value.RedactSensitiveMetadata);
            return Results.Ok(state.Players);
        });
        endpoints.MapGet("/api/events", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var state = MaybeRedact(store.GetState(), options.Value.RedactSensitiveMetadata);
            return Results.Ok(state.RecentEvents);
        });
        endpoints.MapGet("/snapshot", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var state = MaybeRedact(store.GetState(), options.Value.RedactSensitiveMetadata);
            return BuildOverlaySnapshot(state);
        });
        endpoints.MapGet("/eventsrecent", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var state = MaybeRedact(store.GetState(), options.Value.RedactSensitiveMetadata);
            return Results.Ok(state.RecentHistory);
        });
        endpoints.MapGet("/events/recent", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var state = MaybeRedact(store.GetState(), options.Value.RedactSensitiveMetadata);
            return Results.Ok(state.RecentHistory);
        });
        endpoints.MapGet("/api/history", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var state = MaybeRedact(store.GetState(), options.Value.RedactSensitiveMetadata);
            return Results.Ok(state.RecentHistory);
        });
        endpoints.MapGet("/api/saves/latest", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var save = MaybeRedact(store.GetState().Save, options.Value.RedactSensitiveMetadata);
            return Results.Ok(save);
        });
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
        endpoints.MapGet("/api/saves/latest/record-graph", (IWindroseStateStore store) =>
        {
            var save = store.GetState().Save;
            return Results.Ok(new
            {
                readOnly = save.RecordGraph.ReadOnly,
                save.RecordGraph.SourcePath,
                save.RecordGraph.HasCrossLinkedIdentityAndPortableData,
                save.RecordGraph.CanExportWithoutRekey,
                save.RecordGraph.Verdict,
                save.RecordGraph.RecordTypes,
                save.RecordGraph.IdentityMarkers,
                save.RecordGraph.CandidatePortableMarkers,
                save.RecordGraph.ReferenceMarkers,
                save.RecordGraph.CoLocatedEvidence,
                save.RecordGraph.Entries
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
        endpoints.MapGet("/api/server/description", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var state = MaybeRedact(store.GetState(), options.Value.RedactSensitiveMetadata);
            var description = state.ServerDescription ?? state.Save.ServerDescription;
            return description is null
                ? Results.NotFound()
                : Results.Ok(description);
        });
        endpoints.MapGet("/api/world/description", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var state = MaybeRedact(store.GetState(), options.Value.RedactSensitiveMetadata);
            var save = state.Save;
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
        endpoints.MapGet("/api/world/entities", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var save = MaybeRedact(store.GetState().Save, options.Value.RedactSensitiveMetadata);
            return Results.Ok(BuildWorldSlice(save, ["island", "actor", "player-in-world-metadata", "ship-reference"], false));
        });
        endpoints.MapGet("/api/world/players", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var save = MaybeRedact(store.GetState().Save, options.Value.RedactSensitiveMetadata);
            return Results.Ok(BuildWorldSlice(save, ["player-in-world-metadata"], false));
        });
        endpoints.MapGet("/api/world/ships", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var save = MaybeRedact(store.GetState().Save, options.Value.RedactSensitiveMetadata);
            return Results.Ok(BuildWorldSlice(save, ["ship-reference", "ship-document"], false));
        });
        endpoints.MapGet("/api/world/actors", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var save = MaybeRedact(store.GetState().Save, options.Value.RedactSensitiveMetadata);
            return Results.Ok(BuildWorldSlice(save, ["actor"], false));
        });
        endpoints.MapGet("/api/world/summary", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var state = MaybeRedact(store.GetState(), options.Value.RedactSensitiveMetadata);
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
        endpoints.MapGet("/api/runtime/control-surface", () =>
        {
            return Results.Ok(new
            {
                readOnly = true,
                observerSurface = "Windrose State Web",
                executionSurface = "WindrosePlus",
                approvalSurface = "ChannelCheevos / Hermes",
                actionCapabilityReport = "/api/runtime/action-capabilities",
                supportedNow = new[]
                {
                    "log and save observation",
                    "overlay / summary snapshots",
                    "live status push",
                    "auditable operator request records"
                },
                deferred = new[]
                {
                    "chat injection",
                    "entity spawning",
                    "generic world mutation"
                },
                contract = new
                {
                    request = "ChannelCheevos / Hermes requests the action",
                    approval = "Operator authorizes or revokes the request",
                    execution = "WindrosePlus performs the live action",
                    audit = "Every request and result is logged"
                }
            });
        });
        endpoints.MapGet("/api/runtime/action-capabilities", () => Results.Ok(BuildRuntimeActionCapabilityReport()));
        endpoints.MapGet("/api/history/export", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var state = MaybeRedact(store.GetState(), options.Value.RedactSensitiveMetadata);
            return Results.Ok(state.ToHistoryExport(DateTimeOffset.UtcNow));
        });
        endpoints.MapGet("/api/history/timeseries", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var state = MaybeRedact(store.GetState(), options.Value.RedactSensitiveMetadata);
            return Results.Ok(BuildTimeSeriesExport(state));
        });
        endpoints.MapGet("/api/overlay/summary", (IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            var state = MaybeRedact(store.GetState(), options.Value.RedactSensitiveMetadata);
            return Results.Ok(BuildOverlaySnapshot(state));
        });

        endpoints.MapGet("/api/events/stream", async (HttpContext context, IWindroseStateStore store, IOptions<WindroseStateOptions> options) =>
        {
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Content-Type", "text/event-stream");

            var reader = store.Subscribe(context.RequestAborted);
            var redact = options.Value.RedactSensitiveMetadata;
            await foreach (var evt in reader.ReadAllAsync(context.RequestAborted))
            {
                var payload = redact ? RedactEvent(evt) : evt;
                await context.Response.WriteAsync($"event: {payload.Type}\n", context.RequestAborted);
                await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(payload)}\n\n", context.RequestAborted);
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

    private static WindroseServerState MaybeRedact(WindroseServerState state, bool redactSensitiveMetadata)
    {
        return redactSensitiveMetadata ? RedactState(state) : state;
    }

    private static SaveMetadata MaybeRedact(SaveMetadata save, bool redactSensitiveMetadata)
    {
        return redactSensitiveMetadata ? RedactSaveMetadata(save) : save;
    }

    private static WindroseServerState RedactState(WindroseServerState state)
    {
        return state with
        {
            ServerName = RedactString(state.ServerName),
            InviteCode = RedactString(state.InviteCode),
            ServerDescription = RedactServerDescription(state.ServerDescription),
            Save = RedactSaveMetadata(state.Save),
            Players = state.Players.Select(RedactPlayer).ToArray(),
            RecentEvents = state.RecentEvents.Select(RedactEvent).ToArray(),
            RecentWarnings = state.RecentWarnings.Select(RedactEvent).ToArray(),
            RecentHistory = state.RecentHistory.Select(RedactHistoryEntry).ToArray()
        };
    }

    private static SaveMetadata RedactSaveMetadata(SaveMetadata save)
    {
        return save with
        {
            ServerDescription = RedactServerDescription(save.ServerDescription)
        };
    }

    private static ServerDescriptionMetadata? RedactServerDescription(ServerDescriptionMetadata? description)
    {
        return description is null
            ? null
            : description with
            {
                ServerName = RedactString(description.ServerName),
                InviteCode = RedactString(description.InviteCode)
            };
    }

    private static PlayerConnectionState RedactPlayer(PlayerConnectionState player)
    {
        return player with
        {
            SessionId = RedactString(player.SessionId),
            AccountId = RedactString(player.AccountId),
            ClientName = RedactString(player.ClientName)
        };
    }

    private static WindroseEvent RedactEvent(WindroseEvent evt)
    {
        return evt with
        {
            SessionId = RedactString(evt.SessionId),
            AccountId = RedactString(evt.AccountId),
            ClientName = RedactString(evt.ClientName)
        };
    }

    private static WindroseTimelineEntry RedactHistoryEntry(WindroseTimelineEntry entry)
    {
        return entry with
        {
            SessionId = RedactString(entry.SessionId),
            AccountId = RedactString(entry.AccountId),
            ClientName = RedactString(entry.ClientName)
        };
    }

    private static string? RedactString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? value : "[redacted]";
    }

    private static object BuildRuntimeActionCapabilityReport()
    {
        var knownActionIds = RuntimeActionCapabilities.Select(action => action.Id).ToArray();
        var unsupportedActions = RuntimeActionCapabilities.Select(action => new
        {
            action.Id,
            action.EventName,
            action.DisplayName,
            action.Status,
            action.Reason,
            hookContract = action.HookContract
        }).ToArray();

        return new
        {
            readOnly = true,
            source = "ChannelCheevos WindroseActionCatalog",
            observerSurface = "Windrose State Web",
            executionSurface = "WindrosePlus",
            approvalSurface = "ChannelCheevos / Hermes",
            knownCount = knownActionIds.Length,
            enabledCount = 0,
            disabledCount = 0,
            unsupportedCount = unsupportedActions.Length,
            knownActionIds,
            enabledActionIds = Array.Empty<string>(),
            disabledActionIds = Array.Empty<string>(),
            unsupportedActions,
            contract = new
            {
                request = "ChannelCheevos / Hermes requests the action",
                approval = "Operator authorizes or revokes the request",
                execution = "WindrosePlus performs the live action when a proven hook exists",
                audit = "Every request and result is logged"
            }
        };
    }

    private sealed record RuntimeActionCapabilityDefinition(
        string Id,
        string EventName,
        string DisplayName,
        string Status,
        string Reason,
        RuntimeActionHookContract? HookContract = null);

    private sealed record RuntimeActionHookContract(
        string Seam,
        string TargetSelector,
        string[] PayloadFields,
        string DryRunOutput,
        string[] FailureModes);

    private static readonly RuntimeActionCapabilityDefinition[] RuntimeActionCapabilities =
    {
        new(
            "windrose.buff.speed_boost",
            "windrose_action_buff_speed_boost",
            "Speed Boost",
            "unsupported",
            "No native hook or first-class runtime API is proven for player buff mutation in the current runtime."),
        new(
            "windrose.buff.regen_boost",
            "windrose_action_buff_regen_boost",
            "Regen Boost",
            "unsupported",
            "No native hook or first-class runtime API is proven for player buff mutation in the current runtime."),
        new(
            "windrose.weather.storm_front",
            "windrose_action_weather_storm_front",
            "Storm Front",
            "unsupported",
            "Weather control is not exposed as a stable first-class API in the current runtime."),
        new(
            "windrose.world.toggle_safe_mode",
            "windrose_action_world_toggle_safe_mode",
            "Toggle Safe Mode",
            "unsupported",
            "No stable world-toggle command is documented in the current runtime-control surface."),
        new(
            "windrose.spawn.loot_drop",
            "windrose_action_spawn_loot_drop",
            "Loot Drop",
            "unsupported",
            "Spawn paths remain native-hook only until a proven WindrosePlus or upstream API exists."),
        new(
            "windrose.spawn.dodo_swarm",
            "windrose_action_spawn_dodo_swarm",
            "Dodo Swarm",
            "unsupported",
            "Native hook only until the plugin-server bridge can resolve a target player and emit a typed spawn request.",
            new RuntimeActionHookContract(
                "HandleDodoSwarm",
                "targetPlayer",
                ["targetPlayer", "count", "radiusMeters", "offsetMeters", "creatureId", "creatureName", "summon"],
                "Dry run should log the resolved target, count, radius/offset, summon selection mode, creature id/name, and whether the hook was skipped or rejected.",
                ["unknown target player", "invalid count or spawn radius", "hook unavailable", "unsafe live server state", "live execution without approval"])),
        new(
            "windrose.cosmetic.confetti",
            "windrose_action_cosmetic_confetti",
            "Confetti",
            "unsupported",
            "Cosmetic effects are not exposed as a stable first-class runtime API in this slice."),
        new(
            "windrose.system.emergency_stop",
            "windrose_action_system_emergency_stop",
            "Emergency Stop",
            "unsupported",
            "No live stop or kill control is exposed by the current runtime-control surface."),
        new(
            "windrose.system.catalog_sync",
            "windrose_action_system_catalog_sync",
            "Catalog Sync",
            "unsupported",
            "Manifest sync is a contract-only handshake and not an execution action in this runtime.")
    };

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
