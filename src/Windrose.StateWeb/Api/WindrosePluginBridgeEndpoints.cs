using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Windrose.StateWeb.Options;

namespace Windrose.StateWeb.Api;

public static class WindrosePluginBridgeEndpoints
{
    private const string PluginId = "windrose-sidecar-bridge";
    private const string PluginDisplayName = "Windrose Sidecar Bridge";
    private const int DefaultMaxTeleportersPerIsland = 3;
    private const int DefaultRequestedStackSizeMultiplier = 1;
    private const string StackSizeEnforcement = "disabled-upstream-no-live-write";
    private static readonly CreatureDefinition[] AllowedSummonCreatures =
    [
        new("R5.Creature.Dodo", "Dodo"),
        new("R5.Creature.Wolf", "Wolf")
    ];

    public static IEndpointRouteBuilder MapWindrosePluginBridgeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/plugin/manifest", (IOptions<WindroseStateOptions> options) =>
        {
            var bridge = BuildBridgePaths(options.Value);
            var effectiveLimits = ReadPluginConfigLimits(options.Value) ?? new WindrosePluginLimits(
                DefaultMaxTeleportersPerIsland,
                DefaultRequestedStackSizeMultiplier,
                StackSizeEnforcement);
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
                    smokeOptions = "/api/plugin/smoke-options",
                    dryRun = "/api/plugin/actions/dry-run",
                    execute = "/api/plugin/actions/execute",
                    result = "/api/plugin/actions/{actionRequestId}/result"
                },
                config = new
                {
                    file = "server-files/windrose_plugin_bridge/config.json",
                    environmentVariables = new[]
                    {
                        "WINDROSE_MAX_TELEPORTERS_PER_ISLAND",
                        "WINDROSE_REQUESTED_STACK_SIZE_MULTIPLIER"
                    },
                    limits = new
                    {
                        maxTeleportersPerIsland = effectiveLimits.MaxTeleportersPerIsland ?? DefaultMaxTeleportersPerIsland,
                        requestedStackSizeMultiplier = effectiveLimits.RequestedStackSizeMultiplier ?? DefaultRequestedStackSizeMultiplier,
                        allowedStackSizeMultipliers = new[] { 1, 2, 3 },
                        stackSizeEnforcement = effectiveLimits.StackSizeEnforcement ?? StackSizeEnforcement
                    },
                    enforcement = "contract-only-until-native-hooks-are-proven",
                    notes = new[]
                    {
                        "Teleporter cap is config/heartbeat only until a native island placement/counting hook is proven.",
                        "Stack size multiplier is request/contract only; legacy stack_size writes stay disabled because upstream inventory mutation can corrupt save state."
                    }
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
                            "creatureName",
                            "summon"
                        },
                        allowedCreatureNames = AllowedSummonCreatures.Select(creature => creature.Name).ToArray(),
                        allowedCreatureIds = AllowedSummonCreatures.Select(creature => creature.Id).ToArray(),
                        summonContract = new
                        {
                            objectField = "summon",
                            selectionModes = new[] { "explicit", "random" },
                            fields = new[] { "creatureId", "creatureName", "creature", "selection", "creaturePool", "count", "radiusMeters", "offsetMeters" },
                            randomSentinel = "random",
                            defaultCreaturePool = AllowedSummonCreatures.Select(creature => creature.Name).ToArray()
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

        endpoints.MapGet("/api/plugin/smoke-options", () =>
        {
            return Results.Ok(new
            {
                pluginId = PluginId,
                readOnly = true,
                matrix = "docs/roadmaps/windrose-runtime-control-surface/safe-smoke-harness-matrix.md",
                globalRules = new[]
                {
                    "Dev server only for any smoke that touches a live Windrose runtime or player state.",
                    "Player-bound smokes default to a non-main / throwaway character.",
                    "Random player testing is read-only only unless explicit consent exists.",
                    "Mutating smokes require approval, exact target identity, log capture, and a rollback or revert plan."
                },
                modes = new[]
                {
                    new
                    {
                        modeId = "offline-mock-player",
                        allowedTarget = "Local fixture, disposable harness, or mocked player object.",
                        risk = "low",
                        readOnly = true,
                        approvalRequired = false,
                        blockIfMutationRequested = true,
                        evidence = new[] { "harness output", "fixture snapshot", "pass/fail result" }
                    },
                    new
                    {
                        modeId = "dev-server-no-player",
                        allowedTarget = "Dev server with no connected players.",
                        risk = "low",
                        readOnly = true,
                        approvalRequired = false,
                        blockIfMutationRequested = true,
                        evidence = new[] { "server/bridge status", "manifest or health response", "logs showing read-only path" }
                    },
                    new
                    {
                        modeId = "random-online-dev-player-read-only",
                        allowedTarget = "Any connected dev player, read-only probes only.",
                        risk = "low",
                        readOnly = true,
                        approvalRequired = false,
                        blockIfMutationRequested = true,
                        evidence = new[] { "probe output", "status response", "confirmation that no writes occurred" }
                    },
                    new
                    {
                        modeId = "operator-non-main-character",
                        allowedTarget = "Dev server, clearly named non-main / throwaway character.",
                        risk = "medium",
                        readOnly = false,
                        approvalRequired = true,
                        blockIfMutationRequested = false,
                        evidence = new[] { "pre/post state", "command log", "rollback record", "timestamps" }
                    },
                    new
                    {
                        modeId = "consenting-dev-player",
                        allowedTarget = "Dev server, explicitly consenting player account.",
                        risk = "medium-high",
                        readOnly = false,
                        approvalRequired = true,
                        blockIfMutationRequested = false,
                        evidence = new[] { "consent record", "pre/post state", "logs", "timestamps" }
                    },
                    new
                    {
                        modeId = "sidecar-plugin-down-failure",
                        allowedTarget = "Dev stack or local harness with plugin or sidecar intentionally disabled.",
                        risk = "low",
                        readOnly = true,
                        approvalRequired = false,
                        blockIfMutationRequested = true,
                        evidence = new[] { "graceful failure message", "degraded-mode behavior", "no fallback write path" }
                    },
                    new
                    {
                        modeId = "plugin-reload",
                        allowedTarget = "Approved dev stack or local harness while reloading the bridge/plugin boundary.",
                        risk = "medium",
                        readOnly = false,
                        approvalRequired = false,
                        blockIfMutationRequested = false,
                        evidence = new[] { "reload log", "status after reload", "no fallback write path" }
                    },
                    new
                    {
                        modeId = "malformed-command",
                        allowedTarget = "Dev harness or bridge endpoint with an intentionally invalid payload or unknown action.",
                        risk = "low",
                        readOnly = true,
                        approvalRequired = false,
                        blockIfMutationRequested = true,
                        evidence = new[] { "rejected response", "validation error", "no action file written" }
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
                    status?.Limits,
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

        endpoints.MapGet("/api/plugin/events/recent", async (IOptions<WindroseStateOptions> options, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("WindrosePluginBridgeEndpoints");
            var eventsPath = options.Value.PluginBridgeEventsPath;
            if (!Directory.Exists(eventsPath))
            {
                return Results.Ok(new
                {
                    pluginId = PluginId,
                    eventsPath,
                    count = 0,
                    events = Array.Empty<JsonElement>()
                });
            }

            var eventFiles = Directory
                .EnumerateFiles(eventsPath, "*.json", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ThenByDescending(Path.GetFileName)
                .ToArray();

            var events = new List<JsonElement>(eventFiles.Length);
            foreach (var eventFile in eventFiles)
            {
                try
                {
                    await using var stream = File.OpenRead(eventFile);
                    using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                    events.Add(document.RootElement.Clone());
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Skipping invalid Windrose bridge event file {EventFile}", eventFile);
                }
                catch (IOException ex)
                {
                    logger.LogWarning(ex, "Skipping unreadable Windrose bridge event file {EventFile}", eventFile);
                }
            }

            return Results.Ok(new
            {
                pluginId = PluginId,
                eventsPath,
                count = events.Count,
                events = events
            });
        });

        endpoints.MapPost("/api/plugin/actions/dry-run", async (HttpRequest httpRequest, CancellationToken cancellationToken) =>
        {
            var parsed = await ReadActionRequestAsync(httpRequest, dryRun: true, cancellationToken);
            if (parsed.Error is not null)
            {
                return parsed.Error;
            }

            var request = parsed.Request!;
            var summon = NormalizeSummon(request);
            var validationErrors = Validate(request, summon);
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

            var creature = ResolveCreature(summon);
            var logLine = $"[windrose-sidecar-bridge] dry-run HandleDodoSwarm target={request.TargetPlayer!.Trim()} count={summon.Count} radiusMeters={summon.RadiusMeters:0.##} offsetMeters={summon.OffsetMeters:0.##} selectionMode={summon.SelectionMode} creatureId={creature.Id} creatureName={creature.Name} result=not-executed approvalRequired=true";

            return Results.Ok(BuildValidatedActionResponse(request, summon, creature, dryRun: true, queued: false, actionRequestId: null, outcome: "validated-dry-run-only", logLine));
        });

        endpoints.MapPost("/api/plugin/actions/execute", async (HttpRequest httpRequest, IOptions<WindroseStateOptions> options, CancellationToken cancellationToken) =>
        {
            var parsed = await ReadActionRequestAsync(httpRequest, dryRun: false, cancellationToken);
            if (parsed.Error is not null)
            {
                return parsed.Error;
            }

            var request = parsed.Request!;
            var summon = NormalizeSummon(request);
            var validationErrors = Validate(request, summon);
            if (!options.Value.PluginBridgeDevExecutionEnabled)
            {
                validationErrors.Add("Dev execution gate is disabled. Set WindroseState__PluginBridgeDevExecutionEnabled=true only on the approved windrose2-dev test bed.");
            }

            if (string.IsNullOrWhiteSpace(request.ApprovalId))
            {
                validationErrors.Add("approvalId is required for dev execution.");
            }

            if (!string.Equals(request.ModeId, "operator-non-main-character", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.ModeId, "consenting-dev-player", StringComparison.OrdinalIgnoreCase))
            {
                validationErrors.Add("modeId must be operator-non-main-character or consenting-dev-player for dev execution.");
            }

            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(new
                {
                    pluginId = PluginId,
                    accepted = false,
                    dryRun = false,
                    queued = false,
                    executed = false,
                    errors = validationErrors
                });
            }

            Directory.CreateDirectory(options.Value.PluginBridgeActionsPath);
            Directory.CreateDirectory(options.Value.PluginBridgeResultsPath);
            var creature = ResolveCreature(summon);
            var actionRequestId = $"wr-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
            var actionPath = Path.Combine(options.Value.PluginBridgeActionsPath, $"{actionRequestId}.json");
            var actionPayload = BuildQueuedActionPayload(request, summon, creature, actionRequestId);
            await File.WriteAllTextAsync(actionPath, JsonSerializer.Serialize(actionPayload, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }), cancellationToken);
            await File.AppendAllTextAsync(Path.Combine(options.Value.PluginBridgeActionsPath, "pending.txt"), actionRequestId + Environment.NewLine, cancellationToken);

            var logLine = $"[windrose-sidecar-bridge] queued HandleDodoSwarm actionRequestId={actionRequestId} target={request.TargetPlayer!.Trim()} count={summon.Count} radiusMeters={summon.RadiusMeters:0.##} offsetMeters={summon.OffsetMeters:0.##} selectionMode={summon.SelectionMode} creatureId={creature.Id} creatureName={creature.Name} approvalId={request.ApprovalId!.Trim()} modeId={request.ModeId!.Trim()}";
            return Results.Accepted($"/api/plugin/actions/{actionRequestId}/result", BuildValidatedActionResponse(request, summon, creature, dryRun: false, queued: true, actionRequestId, outcome: "queued-for-dev-plugin-execution", logLine));
        });

        endpoints.MapGet("/api/plugin/actions/{actionRequestId}/result", async (string actionRequestId, IOptions<WindroseStateOptions> options, CancellationToken cancellationToken) =>
        {
            if (!IsSafeActionRequestId(actionRequestId))
            {
                return Results.BadRequest(new
                {
                    pluginId = PluginId,
                    accepted = false,
                    error = "actionRequestId contains unsupported characters."
                });
            }

            var resultPath = Path.Combine(options.Value.PluginBridgeResultsPath, $"{actionRequestId}.json");
            if (!File.Exists(resultPath))
            {
                return Results.Ok(new
                {
                    pluginId = PluginId,
                    actionRequestId,
                    status = "pending",
                    executed = false,
                    resultPath
                });
            }

            try
            {
                await using var stream = File.OpenRead(resultPath);
                var result = await JsonSerializer.DeserializeAsync<JsonElement>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken);
                return Results.Ok(result);
            }
            catch (JsonException ex)
            {
                return Results.Problem($"Plugin result writeback is not valid JSON: {ex.Message}", statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (IOException ex)
            {
                return Results.Problem($"Plugin result writeback could not be read: {ex.Message}", statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });

        return endpoints;
    }

    private static async Task<ActionRequestParseResult> ReadActionRequestAsync(HttpRequest httpRequest, bool dryRun, CancellationToken cancellationToken)
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
            return new ActionRequestParseResult(null, Results.BadRequest(new
            {
                pluginId = PluginId,
                accepted = false,
                dryRun,
                errors = new[] { $"Request body is not valid JSON: {ex.Message}" }
            }));
        }

        if (request is null)
        {
            return new ActionRequestParseResult(null, Results.BadRequest(new
            {
                pluginId = PluginId,
                accepted = false,
                dryRun,
                errors = new[] { "Request body is required." }
            }));
        }

        return new ActionRequestParseResult(request, null);
    }

    private static object BuildValidatedActionResponse(
        WindrosePluginActionDryRunRequest request,
        NormalizedSummonRequest summon,
        CreatureDefinition creature,
        bool dryRun,
        bool queued,
        string? actionRequestId,
        string outcome,
        string logLine) => new
    {
        pluginId = PluginId,
        accepted = true,
        dryRun,
        queued,
        executed = false,
        actionRequestId,
        actionId = request.ActionId,
        handler = "HandleDodoSwarm",
        targetPlayer = request.TargetPlayer!.Trim(),
        summon.Count,
        summon.RadiusMeters,
        summon.OffsetMeters,
        approvalId = request.ApprovalId?.Trim(),
        modeId = request.ModeId?.Trim(),
        summon = new
        {
            selectionMode = summon.SelectionMode,
            creatureId = creature.Id,
            creatureName = creature.Name,
            requestedCreature = summon.RequestedCreature,
            randomCreaturePool = summon.CreaturePool.Select(creature => creature.Name).ToArray()
        },
        creatureId = creature.Id,
        creatureName = creature.Name,
        selectionMode = summon.SelectionMode,
        outcome,
        logLine
    };

    private static object BuildQueuedActionPayload(
        WindrosePluginActionDryRunRequest request,
        NormalizedSummonRequest summon,
        CreatureDefinition creature,
        string actionRequestId) => new
    {
        pluginId = PluginId,
        actionRequestId,
        actionId = request.ActionId.Trim(),
        handler = "HandleDodoSwarm",
        dryRun = false,
        approved = true,
        approvalId = request.ApprovalId!.Trim(),
        modeId = request.ModeId!.Trim(),
        requestedAt = DateTimeOffset.UtcNow,
        targetPlayer = request.TargetPlayer!.Trim(),
        count = summon.Count,
        radiusMeters = summon.RadiusMeters,
        offsetMeters = summon.OffsetMeters,
        selectionMode = summon.SelectionMode,
        creatureId = creature.Id,
        creatureName = creature.Name,
        safety = new
        {
            approvedDevServer = "windrose2-dev",
            playerTargeting = "non-main/throwaway or consenting dev player only",
            productionAllowed = false
        }
    };

    private static bool IsSafeActionRequestId(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 96 &&
        value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');

    private sealed record ActionRequestParseResult(WindrosePluginActionDryRunRequest? Request, IResult? Error);

    private static object BuildBridgePaths(WindroseStateOptions options) => new
    {
        rootPath = options.PluginBridgePath,
        statusPath = options.PluginBridgeStatusPath,
        eventsPath = options.PluginBridgeEventsPath,
        actionsPath = options.PluginBridgeActionsPath,
        resultsPath = options.PluginBridgeResultsPath,
        configPath = options.PluginBridgeConfigPath
    };

    private static WindrosePluginLimits? ReadPluginConfigLimits(WindroseStateOptions options)
    {
        if (!File.Exists(options.PluginBridgeConfigPath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(options.PluginBridgeConfigPath);
            return JsonSerializer.Deserialize<WindrosePluginConfig>(
                stream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))?.Limits;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static List<string> Validate(WindrosePluginActionDryRunRequest request, NormalizedSummonRequest summon)
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

        if (summon.Count is < 1 or > 50)
        {
            errors.Add("count must be between 1 and 50.");
        }

        if (summon.RadiusMeters is < 1 or > 100)
        {
            errors.Add("radiusMeters must be between 1 and 100.");
        }

        if (summon.OffsetMeters is < 0 or > 100)
        {
            errors.Add("offsetMeters must be between 0 and 100.");
        }

        if (summon.InvalidCreatureId is not null)
        {
            errors.Add("creatureId must be R5.Creature.Dodo or R5.Creature.Wolf, or use the random sentinel through summon.selection.");
        }

        if (summon.InvalidCreatureName is not null)
        {
            errors.Add("creatureName must be Dodo or Wolf, or use random through summon.selection/creature.");
        }

        if (summon.CreatureMismatch)
        {
            errors.Add("creatureId and creatureName must refer to the same allowed creature.");
        }

        if (summon.SelectionMode == "random" && summon.CreaturePool.Count == 0)
        {
            errors.Add("summon.creaturePool must contain at least one allowed creature when random selection is requested.");
        }

        return errors;
    }

    private static NormalizedSummonRequest NormalizeSummon(WindrosePluginActionDryRunRequest request)
    {
        var summon = request.Summon;
        var requestedCreature = FirstNonBlank(summon?.Creature, summon?.CreatureName, summon?.CreatureId, request.CreatureName, request.CreatureId);
        var selectionMode = FirstNonBlank(summon?.Selection);
        var wantsRandom = IsRandom(selectionMode) || IsRandom(requestedCreature);
        var explicitCreatureName = IsRandom(summon?.CreatureName) ? null : FirstNonBlank(summon?.CreatureName, summon?.Creature, request.CreatureName);
        var explicitCreatureId = IsRandom(summon?.CreatureId) ? null : FirstNonBlank(summon?.CreatureId, request.CreatureId);

        var pool = NormalizeCreaturePool(summon?.CreaturePool, wantsRandom);
        var idCreature = string.IsNullOrWhiteSpace(explicitCreatureId) ? null : FindCreatureById(explicitCreatureId.Trim());
        var nameCreature = string.IsNullOrWhiteSpace(explicitCreatureName) ? null : FindCreatureByName(explicitCreatureName.Trim());

        return new NormalizedSummonRequest(
            summon?.Count ?? request.Count,
            summon?.RadiusMeters ?? request.RadiusMeters,
            summon?.OffsetMeters ?? request.OffsetMeters,
            wantsRandom ? "random" : "explicit",
            requestedCreature,
            pool,
            idCreature,
            nameCreature,
            !string.IsNullOrWhiteSpace(explicitCreatureId) && idCreature is null ? explicitCreatureId : null,
            !string.IsNullOrWhiteSpace(explicitCreatureName) && nameCreature is null ? explicitCreatureName : null,
            idCreature is not null && nameCreature is not null && !string.Equals(idCreature.Id, nameCreature.Id, StringComparison.OrdinalIgnoreCase));
    }

    private static CreatureDefinition ResolveCreature(NormalizedSummonRequest summon)
    {
        if (summon.SelectionMode == "random")
        {
            return summon.CreaturePool.Count == 0
                ? AllowedSummonCreatures[0]
                : summon.CreaturePool[Random.Shared.Next(summon.CreaturePool.Count)];
        }

        return summon.NameCreature ?? summon.IdCreature ?? AllowedSummonCreatures[0];
    }

    private static IReadOnlyList<CreatureDefinition> NormalizeCreaturePool(string[]? requestedPool, bool wantsRandom)
    {
        if (!wantsRandom)
        {
            return Array.Empty<CreatureDefinition>();
        }

        if (requestedPool is null || requestedPool.Length == 0)
        {
            return AllowedSummonCreatures;
        }

        return requestedPool
            .Select(value => FindCreatureByName(value.Trim()) ?? FindCreatureById(value.Trim()))
            .Where(creature => creature is not null)
            .Select(creature => creature!)
            .DistinctBy(creature => creature.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static bool IsRandom(string? value) =>
        string.Equals(value?.Trim(), "random", StringComparison.OrdinalIgnoreCase);

    private static CreatureDefinition? FindCreatureByName(string creatureName) =>
        AllowedSummonCreatures.FirstOrDefault(creature => string.Equals(creature.Name, creatureName, StringComparison.OrdinalIgnoreCase));

    private static CreatureDefinition? FindCreatureById(string creatureId) =>
        AllowedSummonCreatures.FirstOrDefault(creature => string.Equals(creature.Id, creatureId, StringComparison.OrdinalIgnoreCase));

    private sealed record CreatureDefinition(string Id, string Name);

    private sealed record WindrosePluginStatus(
        string? PluginId,
        string? Status,
        DateTimeOffset? StartedAt,
        string? SidecarUrl,
        string? Mode,
        WindrosePluginLimits? Limits,
        string? Message);

    private sealed record WindrosePluginConfig(WindrosePluginLimits? Limits);

    private sealed record WindrosePluginLimits(
        int? MaxTeleportersPerIsland,
        int? RequestedStackSizeMultiplier,
        string? StackSizeEnforcement);

    private sealed record WindrosePluginActionDryRunRequest(
        string ActionId,
        string? TargetPlayer,
        int Count = 8,
        double RadiusMeters = 8,
        double OffsetMeters = 2,
        string? CreatureId = null,
        string? CreatureName = null,
        SummonObject? Summon = null,
        string? ApprovalId = null,
        string? ModeId = null);

    private sealed record SummonObject(
        string? CreatureId = null,
        string? CreatureName = null,
        string? Creature = null,
        string? Selection = null,
        string[]? CreaturePool = null,
        int? Count = null,
        double? RadiusMeters = null,
        double? OffsetMeters = null);

    private sealed record NormalizedSummonRequest(
        int Count,
        double RadiusMeters,
        double OffsetMeters,
        string SelectionMode,
        string? RequestedCreature,
        IReadOnlyList<CreatureDefinition> CreaturePool,
        CreatureDefinition? IdCreature,
        CreatureDefinition? NameCreature,
        string? InvalidCreatureId,
        string? InvalidCreatureName,
        bool CreatureMismatch);
}
