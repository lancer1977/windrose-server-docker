using System.Text.Json;
using Microsoft.Extensions.Options;
using Windrose.StateWeb.Options;

namespace Windrose.StateWeb.Api;

public static class WindrosePluginBridgeEndpoints
{
    private const string PluginId = "windrose-sidecar-bridge";
    private const string PluginDisplayName = "Windrose Sidecar Bridge";

    public static IEndpointRouteBuilder MapWindrosePluginBridgeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/plugin/manifest", (IOptions<WindroseStateOptions> options) =>
        {
            var bridge = BuildBridgePaths(options.Value);
            return Results.Ok(new
            {
                pluginId = PluginId,
                displayName = PluginDisplayName,
                protocolVersion = "windrose.plugin.sidecar.v1",
                readOnlySidecar = true,
                sidecar = new
                {
                    manifest = "/api/plugin/manifest",
                    status = "/api/plugin/status",
                    dryRun = "/api/plugin/actions/dry-run"
                },
                bridge,
                actions = new[]
                {
                    new
                    {
                        actionId = "windrose.spawn.dodo_swarm",
                        eventName = "windrose_action_spawn_dodo_swarm",
                        handler = "HandleDodoSwarm",
                        mode = "dry-run-only",
                        payloadFields = new[]
                        {
                            "targetPlayer",
                            "count",
                            "radiusMeters",
                            "offsetMeters",
                            "creatureId",
                            "creatureName"
                        },
                        failureModes = new[]
                        {
                            "unknown target player",
                            "invalid count or spawn radius",
                            "hook unavailable",
                            "unsafe live server state",
                            "live execution without approval"
                        }
                    }
                }
            });
        });

        endpoints.MapGet("/api/plugin/status", async (IOptions<WindroseStateOptions> options, CancellationToken cancellationToken) =>
        {
            var statusPath = options.Value.PluginBridgeStatusPath;
            if (!File.Exists(statusPath))
            {
                return Results.Ok(new
                {
                    pluginId = PluginId,
                    connected = false,
                    status = "not-installed-or-not-started",
                    statusPath,
                    message = "No plugin status heartbeat has been written yet. Enable WINDROSE_SIDECAR_PLUGIN_ENABLED and restart Windrose+ to install the bridge plugin."
                });
            }

            try
            {
                await using var stream = File.OpenRead(statusPath);
                var status = await JsonSerializer.DeserializeAsync<WindrosePluginStatus>(
                    stream,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web),
                    cancellationToken);
                return Results.Ok(new
                {
                    pluginId = status?.PluginId ?? PluginId,
                    connected = string.Equals(status?.Status, "started", StringComparison.OrdinalIgnoreCase),
                    status = status?.Status ?? "unknown",
                    statusPath,
                    status?.StartedAt,
                    status?.SidecarUrl,
                    status?.Mode,
                    status?.Message
                });
            }
            catch (JsonException ex)
            {
                return Results.Problem($"Plugin status heartbeat is not valid JSON: {ex.Message}", statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (IOException ex)
            {
                return Results.Problem($"Plugin status heartbeat could not be read: {ex.Message}", statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });

        endpoints.MapPost("/api/plugin/actions/dry-run", async (HttpRequest httpRequest, CancellationToken cancellationToken) =>
        {
            WindrosePluginActionDryRunRequest? request;
            try
            {
                request = await JsonSerializer.DeserializeAsync<WindrosePluginActionDryRunRequest>(
                    httpRequest.Body,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web),
                    cancellationToken);
            }
            catch (JsonException ex)
            {
                return Results.BadRequest(new
                {
                    pluginId = PluginId,
                    accepted = false,
                    dryRun = true,
                    errors = new[] { $"Request body is not valid JSON: {ex.Message}" }
                });
            }

            if (request is null)
            {
                return Results.BadRequest(new
                {
                    pluginId = PluginId,
                    accepted = false,
                    dryRun = true,
                    errors = new[] { "Request body is required." }
                });
            }

            var validationErrors = Validate(request);
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(new
                {
                    pluginId = PluginId,
                    accepted = false,
                    dryRun = true,
                    errors = validationErrors
                });
            }

            var creatureName = string.IsNullOrWhiteSpace(request.CreatureName) ? "Dodo" : request.CreatureName.Trim();
            var creatureId = string.IsNullOrWhiteSpace(request.CreatureId) ? "R5.Creature.Dodo" : request.CreatureId.Trim();
            var logLine = $"[windrose-sidecar-bridge] dry-run HandleDodoSwarm target={request.TargetPlayer!.Trim()} count={request.Count} radiusMeters={request.RadiusMeters:0.##} offsetMeters={request.OffsetMeters:0.##} creatureId={creatureId} creatureName={creatureName} result=not-executed approvalRequired=true";

            return Results.Ok(new
            {
                pluginId = PluginId,
                accepted = true,
                dryRun = true,
                executed = false,
                actionId = request.ActionId,
                handler = "HandleDodoSwarm",
                targetPlayer = request.TargetPlayer.Trim(),
                request.Count,
                request.RadiusMeters,
                request.OffsetMeters,
                creatureId,
                creatureName,
                outcome = "validated-dry-run-only",
                logLine
            });
        });

        return endpoints;
    }

    private static object BuildBridgePaths(WindroseStateOptions options) => new
    {
        rootPath = options.PluginBridgePath,
        statusPath = options.PluginBridgeStatusPath,
        actionsPath = options.PluginBridgeActionsPath,
        resultsPath = options.PluginBridgeResultsPath,
        configPath = options.PluginBridgeConfigPath
    };

    private static List<string> Validate(WindrosePluginActionDryRunRequest request)
    {
        var errors = new List<string>();
        if (!string.Equals(request.ActionId, "windrose.spawn.dodo_swarm", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("actionId must be windrose.spawn.dodo_swarm.");
        }

        if (string.IsNullOrWhiteSpace(request.TargetPlayer))
        {
            errors.Add("targetPlayer is required.");
        }

        if (request.Count is < 1 or > 50)
        {
            errors.Add("count must be between 1 and 50.");
        }

        if (request.RadiusMeters is < 1 or > 100)
        {
            errors.Add("radiusMeters must be between 1 and 100.");
        }

        if (request.OffsetMeters is < 0 or > 100)
        {
            errors.Add("offsetMeters must be between 0 and 100.");
        }

        return errors;
    }

    private sealed record WindrosePluginStatus(
        string? PluginId,
        string? Status,
        DateTimeOffset? StartedAt,
        string? SidecarUrl,
        string? Mode,
        string? Message);

    private sealed record WindrosePluginActionDryRunRequest(
        string ActionId,
        string? TargetPlayer,
        int Count = 8,
        double RadiusMeters = 8,
        double OffsetMeters = 2,
        string? CreatureId = "R5.Creature.Dodo",
        string? CreatureName = "Dodo");
}
