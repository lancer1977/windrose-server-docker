using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Channels;
using Windrose.StateWeb.Api;
using Windrose.StateWeb.Core.Contracts;
using Windrose.StateWeb.Options;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.Services;
using Windrose.StateWeb.State;

namespace Windrose.StateWeb.Tests.Api;

public sealed class WindroseEndpointsTests
{
    [Fact]
    public async Task HealthEndpointReportsHealthyWhenLogIsAvailable()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/health");

        Assert.Contains("\"status\":\"ok\"", body);
        Assert.Contains("\"logAvailable\":true", body);
        Assert.Contains("\"parserStatus\":\"Running\"", body);
    }

    [Fact]
    public async Task ApiStateEndpointReturnsTheCurrentSnapshot()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/api/state");

        Assert.Contains("\"currentIslandId\":\"8D23C893C50A4DAF6390E4E698FC5C8E\"", body);
        Assert.Contains("\"inviteCode\":\"dbcdevs\"", body);
        Assert.Contains("\"players\"", body);
    }

    [Fact]
    public async Task PlayersEndpointReturnsPlayerState()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/api/players");

        Assert.Contains("\"clientName\":\"Test Player\"", body);
        Assert.Contains("\"phase\":\"connected\"", body);
    }

    [Fact]
    public async Task EventsEndpointReturnsRecentEvents()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/api/events");

        Assert.Contains("\"type\":\"PlayerJoined\"", body);
        Assert.Contains("\"severity\":\"Information\"", body);
    }

    [Fact]
    public async Task HistoryEndpointReturnsRecentHistory()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/api/history");

        Assert.Contains("\"category\":\"State\"", body);
        Assert.Contains("\"category\":\"Event\"", body);
        Assert.Contains("\"type\":\"SaveMetadataUpdated\"", body);
        Assert.Contains("\"type\":\"PlayerJoined\"", body);
    }

    [Fact]
    public async Task SnapshotAliasReturnsOverlayFriendlySnapshot()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/snapshot");

        Assert.Contains("\"recentHistoryCount\":2", body);
        Assert.Contains("\"hasStandaloneShipDocument\":false", body);
        Assert.Contains("\"consumer:channel-cheevos", body);
    }

    [Fact]
    public async Task RuntimeControlSurfaceEndpointSummarizesTheReadOnlyBoundary()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/api/runtime/control-surface");

        Assert.Contains("\"readOnly\":true", body);
        Assert.Contains("\"observerSurface\":\"Windrose State Web\"", body);
        Assert.Contains("\"executionSurface\":\"WindrosePlus\"", body);
        Assert.Contains("\"chat injection\"", body);
        Assert.Contains("\"contract\"", body);
    }

    [Fact]
    public async Task RuntimeActionCapabilityReportSeparatesKnownUnsupportedFromEnabledActions()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/api/runtime/action-capabilities");

        Assert.Contains("\"readOnly\":true", body);
        Assert.Contains("\"knownCount\":9", body);
        Assert.Contains("\"enabledCount\":0", body);
        Assert.Contains("\"disabledCount\":0", body);
        Assert.Contains("\"unsupportedCount\":9", body);
        Assert.Contains("\"enabledActionIds\":[]", body);
        Assert.Contains("\"disabledActionIds\":[]", body);
        Assert.Contains("\"status\":\"unsupported\"", body);
        Assert.Contains("\"windrose.spawn.loot_drop\"", body);
        Assert.Contains("\"windrose.spawn.dodo_swarm\"", body);
        Assert.Contains("\"HandleDodoSwarm\"", body);
        Assert.Contains("\"targetPlayer\"", body);
        Assert.Contains("\"radiusMeters\"", body);
        Assert.Contains("\"creatureId\"", body);
        Assert.True(body.Contains("dry run should log the resolved target", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PluginManifestEndpointAdvertisesSidecarBridgeContract()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/api/plugin/manifest");

        Assert.Contains("\"pluginId\":\"windrose-sidecar-bridge\"", body);
        Assert.Contains("\"protocolVersion\":\"windrose.plugin.sidecar.v1\"", body);
        Assert.Contains("\"readOnlySidecar\":true", body);
        Assert.Contains("\"dryRun\":\"/api/plugin/actions/dry-run\"", body);
        Assert.Contains("\"smokeOptions\":\"/api/plugin/smoke-options\"", body);
        Assert.Contains("\"windrose.spawn.dodo_swarm\"", body);
        Assert.Contains("\"mode\":\"dry-run-only\"", body);
        Assert.Contains("\"allowedCreatureNames\":[\"Dodo\",\"Wolf\"]", body);
        Assert.Contains("\"environmentVariables\":[\"WINDROSE_MAX_TELEPORTERS_PER_ISLAND\",\"WINDROSE_REQUESTED_STACK_SIZE_MULTIPLIER\"]", body);
        Assert.Contains("\"maxTeleportersPerIsland\":3", body);
        Assert.Contains("\"requestedStackSizeMultiplier\":1", body);
        Assert.Contains("\"allowedStackSizeMultipliers\":[1,2,3]", body);
        Assert.Contains("\"stackSizeEnforcement\":\"disabled-upstream-no-live-write\"", body);
        Assert.Contains("contract-only-until-native-hooks-are-proven", body);
        Assert.Contains("legacy stack_size writes stay disabled", body);
    }

    [Fact]
    public async Task PluginSmokeOptionsEndpointAdvertisesSafePlayerTargetModes()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/api/plugin/smoke-options");

        Assert.Contains("\"readOnly\":true", body);
        Assert.Contains("\"modeId\":\"offline-mock-player\"", body);
        Assert.Contains("\"modeId\":\"dev-server-no-player\"", body);
        Assert.Contains("\"modeId\":\"random-online-dev-player-read-only\"", body);
        Assert.Contains("\"modeId\":\"operator-non-main-character\"", body);
        Assert.Contains("\"modeId\":\"consenting-dev-player\"", body);
        Assert.Contains("\"modeId\":\"sidecar-plugin-down-failure\"", body);
        Assert.Contains("\"modeId\":\"plugin-reload\"", body);
        Assert.Contains("\"modeId\":\"malformed-command\"", body);
        Assert.Contains("random player testing is read-only only", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("non-main / throwaway", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("blockIfMutationRequested", body);
        Assert.Contains("approvalRequired", body);
        Assert.DoesNotContain("main character is okay", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PluginStatusEndpointReportsMissingHeartbeatAsNotStarted()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/api/plugin/status");

        Assert.Contains("\"connected\":false", body);
        Assert.Contains("\"status\":\"not-installed-or-not-started\"", body);
        Assert.Contains("status.json", body);
    }

    [Fact]
    public async Task PluginStatusEndpointReadsStartedHeartbeat()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"windrose-plugin-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempRoot, "windrose_plugin_bridge"));
        await File.WriteAllTextAsync(
            Path.Combine(tempRoot, "windrose_plugin_bridge", "status.json"),
            """
            {"pluginId":"windrose-sidecar-bridge","status":"started","startedAt":"2026-06-04T00:00:00Z","sidecarUrl":"http://127.0.0.1:8781","mode":"dry-run-only","limits":{"maxTeleportersPerIsland":4,"requestedStackSizeMultiplier":2,"stackSizeEnforcement":"disabled-upstream-no-live-write"},"message":"test heartbeat"}
            """);

        try
        {
            await using var app = CreateApp(serverFilesPath: tempRoot);
            await app.StartAsync();
            var body = await InvokeGetAsync(app, "/api/plugin/status");

            Assert.Contains("\"connected\":true", body);
            Assert.Contains("\"status\":\"started\"", body);
            Assert.Contains("\"mode\":\"dry-run-only\"", body);
            Assert.Contains("\"maxTeleportersPerIsland\":4", body);
            Assert.Contains("\"requestedStackSizeMultiplier\":2", body);
            Assert.Contains("\"stackSizeEnforcement\":\"disabled-upstream-no-live-write\"", body);
            Assert.Contains("test heartbeat", body);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task PluginBridgeRecentEventsEndpointReturnsTypedV3BridgeEvents()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"windrose-plugin-events-test-{Guid.NewGuid():N}");
        var eventsRoot = Path.Combine(tempRoot, "windrose_plugin_bridge", "events");
        Directory.CreateDirectory(eventsRoot);

        await File.WriteAllTextAsync(
            Path.Combine(eventsRoot, "20260605T000000Z-heartbeat.json"),
            """
            {"messageType":"windrose.heartbeat.v3","schemaVersion":"windrose.plugin_sidecar.v3","messageId":"msg-heartbeat","correlationId":"startup","originSurface":"WindrosePlus","targetSurface":"StateWeb","createdAtUtc":"2026-06-05T00:00:00Z","componentId":"windrose-sidecar-bridge","status":"healthy","heartbeatAtUtc":"2026-06-05T00:00:00Z","notes":"plugin loaded in dry-run-only mode"}
            """);
        await File.WriteAllTextAsync(
            Path.Combine(eventsRoot, "20260605T000001Z-readback.json"),
            """
            {"messageType":"windrose.server.state.readback.v3","schemaVersion":"windrose.plugin_sidecar.v3","messageId":"msg-readback","originSurface":"WindrosePlus","targetSurface":"StateWeb","createdAtUtc":"2026-06-05T00:00:01Z","serverId":"windrose-sidecar-bridge","serverName":"Windrose sidecar bridge","isHealthy":true,"observedAtUtc":"2026-06-05T00:00:01Z","onlinePlayers":0,"maxPlayers":10,"currentBiome":"","notes":"read-only status emission"}
            """);

        try
        {
            await using var app = CreateApp(serverFilesPath: tempRoot);
            await app.StartAsync();
            var body = await InvokeGetAsync(app, "/api/plugin/events/recent");

            Assert.Contains("\"count\":2", body);
            Assert.Contains("windrose.heartbeat.v3", body);
            Assert.Contains("windrose.server.state.readback.v3", body);
            Assert.Contains("\"eventsPath\"", body);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task PluginDryRunEndpointValidatesDodoSwarmRequestWithoutExecuting()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokePostJsonAsync(app, "/api/plugin/actions/dry-run", """
        {
          "actionId": "windrose.spawn.dodo_swarm",
          "targetPlayer": "Test Player",
          "count": 12,
          "radiusMeters": 18,
          "offsetMeters": 3,
          "creatureId": "R5.Creature.Dodo",
          "creatureName": "Dodo"
        }
        """);

        Assert.Contains("\"accepted\":true", body);
        Assert.Contains("\"dryRun\":true", body);
        Assert.Contains("\"executed\":false", body);
        Assert.Contains("\"handler\":\"HandleDodoSwarm\"", body);
        Assert.Contains("approvalRequired=true", body);
    }

    [Fact]
    public async Task PluginDryRunEndpointAllowsWolfSummonByCreatureName()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokePostJsonAsync(app, "/api/plugin/actions/dry-run", """
        {
          "actionId": "windrose.spawn.dodo_swarm",
          "targetPlayer": "Test Player",
          "count": 2,
          "radiusMeters": 10,
          "offsetMeters": 4,
          "creatureName": "wolf"
        }
        """);

        Assert.Contains("\"accepted\":true", body);
        Assert.Contains("\"creatureId\":\"R5.Creature.Wolf\"", body);
        Assert.Contains("\"creatureName\":\"Wolf\"", body);
        Assert.Contains("creatureId=R5.Creature.Wolf creatureName=Wolf", body);
    }

    [Fact]
    public async Task PluginDryRunEndpointAcceptsNestedRandomSummonObject()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokePostJsonAsync(app, "/api/plugin/actions/dry-run", """
        {
          "actionId": "windrose.spawn.dodo_swarm",
          "targetPlayer": "Test Player",
          "summon": {
            "selection": "random",
            "creaturePool": ["Dodo", "Wolf"],
            "count": 3,
            "radiusMeters": 12,
            "offsetMeters": 5
          }
        }
        """);

        Assert.Contains("\"accepted\":true", body);
        Assert.Contains("\"count\":3", body);
        Assert.Contains("\"radiusMeters\":12", body);
        Assert.Contains("\"offsetMeters\":5", body);
        Assert.Contains("\"selectionMode\":\"random\"", body);
        Assert.Contains("\"randomCreaturePool\":[\"Dodo\",\"Wolf\"]", body);
        Assert.Contains("selectionMode=random", body);
    }

    [Fact]
    public async Task PluginDryRunEndpointRejectsRandomSummonWithUnsupportedPool()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokePostJsonAsync(app, "/api/plugin/actions/dry-run", """
        {
          "actionId": "windrose.spawn.dodo_swarm",
          "targetPlayer": "Test Player",
          "summon": {
            "selection": "random",
            "creaturePool": ["Bear"]
          }
        }
        """);

        Assert.Contains("\"accepted\":false", body);
        Assert.Contains("summon.creaturePool must contain at least one allowed creature", body);
    }

    [Fact]
    public async Task PluginDryRunEndpointRejectsUnsupportedSummonCreature()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokePostJsonAsync(app, "/api/plugin/actions/dry-run", """
        {
          "actionId": "windrose.spawn.dodo_swarm",
          "targetPlayer": "Test Player",
          "count": 2,
          "radiusMeters": 10,
          "offsetMeters": 4,
          "creatureName": "bear"
        }
        """);

        Assert.Contains("\"accepted\":false", body);
        Assert.Contains("creatureName must be Dodo or Wolf", body);
    }

    [Fact]
    public async Task PluginDryRunEndpointRejectsInvalidDodoSwarmRequest()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokePostJsonAsync(app, "/api/plugin/actions/dry-run", """
        {
          "actionId": "windrose.spawn.dodo_swarm",
          "targetPlayer": "",
          "count": 0,
          "radiusMeters": 0,
          "offsetMeters": -1
        }
        """);

        Assert.Contains("\"accepted\":false", body);
        Assert.Contains("targetPlayer is required", body);
        Assert.Contains("count must be between 1 and 50", body);
    }

    [Fact]
    public async Task PluginExecuteEndpointRejectsWhenDevExecutionGateIsDisabled()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"windrose-plugin-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            await using var app = CreateApp(serverFilesPath: tempRoot, pluginBridgeDevExecutionEnabled: false);
            await app.StartAsync();
            var body = await InvokePostJsonAsync(app, "/api/plugin/actions/execute", """
            {
              "actionId": "windrose.spawn.dodo_swarm",
              "targetPlayer": "Dev Throwaway",
              "count": 1,
              "approvalId": "operator-approved-dev-smoke",
              "modeId": "operator-non-main-character"
            }
            """);

            Assert.Contains("\"accepted\":false", body);
            Assert.Contains("\"executed\":false", body);
            Assert.Contains("dev execution gate is disabled", body, StringComparison.OrdinalIgnoreCase);
            Assert.False(Directory.Exists(Path.Combine(tempRoot, "windrose_plugin_bridge", "actions")));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task PluginExecuteEndpointQueuesApprovedDevActionFile()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"windrose-plugin-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            await using var app = CreateApp(serverFilesPath: tempRoot, pluginBridgeDevExecutionEnabled: true);
            await app.StartAsync();
            var body = await InvokePostJsonAsync(app, "/api/plugin/actions/execute", """
            {
              "actionId": "windrose.spawn.dodo_swarm",
              "targetPlayer": "Dev Throwaway",
              "count": 1,
              "radiusMeters": 6,
              "offsetMeters": 2,
              "creatureName": "Dodo",
              "approvalId": "operator-approved-dev-smoke",
              "modeId": "operator-non-main-character"
            }
            """);

            Assert.Contains("\"accepted\":true", body);
            Assert.Contains("\"queued\":true", body);
            Assert.Contains("\"dryRun\":false", body);
            Assert.Contains("\"executed\":false", body);
            Assert.Contains("\"approvalId\":\"operator-approved-dev-smoke\"", body);
            Assert.Contains("\"modeId\":\"operator-non-main-character\"", body);

            var actionsPath = Path.Combine(tempRoot, "windrose_plugin_bridge", "actions");
            var actionPath = Assert.Single(Directory.GetFiles(actionsPath, "*.json"));
            var actionFile = await File.ReadAllTextAsync(actionPath);
            Assert.Contains("\"pluginId\": \"windrose-sidecar-bridge\"", actionFile);
            Assert.Contains("\"dryRun\": false", actionFile);
            Assert.Contains("\"approved\": true", actionFile);
            Assert.Contains("\"targetPlayer\": \"Dev Throwaway\"", actionFile);
            Assert.Contains("\"creatureName\": \"Dodo\"", actionFile);
            Assert.Contains("\"approvalId\": \"operator-approved-dev-smoke\"", actionFile);

            var pendingIndex = await File.ReadAllTextAsync(Path.Combine(actionsPath, "pending.txt"));
            Assert.Contains(Path.GetFileNameWithoutExtension(actionPath), pendingIndex);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task PluginActionResultEndpointReadsPluginWriteback()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"windrose-plugin-test-{Guid.NewGuid():N}");
        var resultsPath = Path.Combine(tempRoot, "windrose_plugin_bridge", "results");
        Directory.CreateDirectory(resultsPath);
        await File.WriteAllTextAsync(Path.Combine(resultsPath, "action-123.json"), """
        {"pluginId":"windrose-sidecar-bridge","actionRequestId":"action-123","status":"executed","executed":true,"outcome":"dev-execution-writeback","targetPlayer":"Dev Throwaway"}
        """);

        try
        {
            await using var app = CreateApp(serverFilesPath: tempRoot, pluginBridgeDevExecutionEnabled: true);
            await app.StartAsync();
            var body = await InvokeGetWithRouteValueAsync(app, "/api/plugin/actions/{actionRequestId}/result", "/api/plugin/actions/action-123/result", "actionRequestId", "action-123");

            Assert.Contains("\"actionRequestId\":\"action-123\"", body);
            Assert.Contains("\"status\":\"executed\"", body);
            Assert.Contains("\"executed\":true", body);
            Assert.Contains("Dev Throwaway", body);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task HubEndpointIsMapped()
    {
        await using var app = CreateApp();
        await app.StartAsync();

        var endpoint = FindEndpoint(app.Services, "/hubs/windrose-state");

        Assert.Equal("/hubs/windrose-state", endpoint.RoutePattern.RawText);
    }

    [Fact]
    public async Task RecentEventsAliasReturnsRecentHistory()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/eventsrecent");

        Assert.Contains("\"category\":\"State\"", body);
        Assert.Contains("\"category\":\"Event\"", body);
    }

    [Fact]
    public async Task RecentEventsSlashAliasReturnsRecentHistory()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/events/recent");

        Assert.Contains("\"category\":\"State\"", body);
        Assert.Contains("\"category\":\"Event\"", body);
    }

    [Fact]
    public async Task LatestSaveEndpointReturnsSaveMetadata()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/api/saves/latest");

        Assert.Contains("\"worldName\":\"Test World\"", body);
        Assert.Contains("\"checkpointContainerFormat\":\"RocksDB block-based SST\"", body);
    }

    [Fact]
    public async Task ServerDescriptionEndpointReturnsServerDescription()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/api/server/description");

        Assert.Contains("\"inviteCode\":\"dbcdevs\"", body);
        Assert.Contains("\"serverName\":\"Windrose Test\"", body);
    }

    [Fact]
    public async Task RedactionModeMasksSensitiveMetadata()
    {
        await using var app = CreateApp(redactSensitiveMetadata: true);
        await app.StartAsync();

        var stateBody = await InvokeGetAsync(app, "/api/state");
        var serverDescriptionBody = await InvokeGetAsync(app, "/api/server/description");
        var snapshotBody = await InvokeGetAsync(app, "/snapshot");

        Assert.DoesNotContain("Test Player", stateBody);
        Assert.DoesNotContain("dbcdevs", stateBody);
        Assert.Contains("[redacted]", stateBody);
        Assert.Contains("[redacted]", serverDescriptionBody);
        Assert.Contains("[redacted]", snapshotBody);
    }

    [Fact]
    public async Task WorldDescriptionEndpointReturnsWorldSummary()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/api/world/description");

        Assert.Contains("\"worldName\":\"Test World\"", body);
        Assert.Contains("\"worldPresetType\":\"Custom\"", body);
    }

    [Fact]
    public async Task CheckpointEndpointExposesReadOnlySummary()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var endpoint = FindEndpoint(app.Services, "/api/saves/latest/checkpoint");
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/saves/latest/checkpoint";
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("\"checkpointContainerFormat\":\"RocksDB block-based SST\"", body);
        Assert.Contains("\"readOnly\":true", body);
        Assert.Contains("\"readableTokens\"", body);
    }

    [Fact]
    public async Task HistoryExportEndpointExposesExportWrapper()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var endpoint = FindEndpoint(app.Services, "/api/history/export");
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/history/export";
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("\"entryCount\":2", body);
        Assert.Contains("\"entries\"", body);
        Assert.Contains("\"SaveMetadataUpdated\"", body);
    }

    [Fact]
    public async Task TimeSeriesEndpointExposesReplaySamples()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var endpoint = FindEndpoint(app.Services, "/api/history/timeseries");
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/history/timeseries";
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("\"sampleCount\":2", body);
        Assert.Contains("\"connectedPlayerCount\":0", body);
        Assert.Contains("\"connectedPlayerCount\":1", body);
        Assert.Contains("\"historyCount\":2", body);
    }

    [Fact]
    public async Task ObservedFamiliesEndpointExposesSafeFamilySummary()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var endpoint = FindEndpoint(app.Services, "/api/saves/latest/observed-families");
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/saves/latest/observed-families";
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("\"readOnly\":true", body);
        Assert.Contains("\"hasStandaloneShipDocument\":false", body);
        Assert.Contains("\"observedFamilies\"", body);
        Assert.Contains("\"ship-document\"", body);
    }

    [Fact]
    public async Task OverlaySummaryEndpointExposesOverlayFriendlySnapshot()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var endpoint = FindEndpoint(app.Services, "/api/overlay/summary");
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/overlay/summary";
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("\"recentHistoryCount\":2", body);
        Assert.Contains("\"hasStandaloneShipDocument\":false", body);
        Assert.Contains("\"consumer:channel-cheevos", body);
    }

    [Theory]
    [InlineData("/api/world/entities", "island")]
    [InlineData("/api/world/players", "player-in-world-metadata")]
    [InlineData("/api/world/ships", "ship-document")]
    [InlineData("/api/world/actors", "actor")]
    public async Task WorldSlicesExposeSafeObservedFamilies(string route, string expectedFamily)
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var endpoint = FindEndpoint(app.Services, route);
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = route;
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("\"readOnly\":true", body);
        Assert.Contains("\"hasDecodedDocuments\":false", body);
        Assert.Contains(expectedFamily, body);
    }

    [Fact]
    public async Task WorldSummaryEndpointExposesOverlayFriendlySafeJson()
    {
        await using var app = CreateApp();
        await app.StartAsync();
        var endpoint = FindEndpoint(app.Services, "/api/world/summary");
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/world/summary";
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("\"readOnly\":true", body);
        Assert.Contains("\"hasDecodedDocuments\":false", body);
        Assert.Contains("\"observedFamilyCount\":5", body);
        Assert.Contains("\"hasStandaloneShipDocument\":false", body);
    }

    [Fact]
    public async Task ChannelCheevosStateEndpointReturnsSecretSafeReadback()
    {
        await using var app = CreateApp(channelCheevosReadback: new ChannelCheevosPollReadback
        {
            Enabled = true,
            Configured = true,
            Target = "dev",
            Endpoint = "https://channel-cheevos.example/api/windrose/state",
            Status = "ok",
            Message = "ChannelCheevos state poll succeeded.",
            ObservedAtUtc = DateTimeOffset.Parse("2026-06-05T00:00:00Z"),
            State = new ChannelCheevosStateSnapshot
            {
                ChannelName = "dbcdevs",
                GeneratedAtUtc = DateTimeOffset.Parse("2026-06-05T00:00:00Z"),
                IsInitialized = true,
                ConnectedFeatures = ["windrose"],
                Stream = new ChannelCheevosStreamSnapshot
                {
                    StreamId = "stream-1",
                    Title = "Windrose smoke",
                    SubscriberCount = 2,
                    ChatterCount = 3
                }
            },
            RawError = "secret webkey must stay internal"
        });
        await app.StartAsync();
        var body = await InvokeGetAsync(app, "/api/channel-cheevos/state");

        Assert.Contains("\"status\":\"ok\"", body);
        Assert.Contains("\"channelName\":\"dbcdevs\"", body);
        Assert.Contains("Windrose smoke", body);
        Assert.DoesNotContain("secret webkey", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RawError", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EventsStreamEndpointWritesServerSentEvents()
    {
        var channel = Channel.CreateUnbounded<WindroseEvent>();
        channel.Writer.TryWrite(new WindroseEvent(DateTimeOffset.Parse("2026-05-21T20:00:00Z"), "PlayerJoined", "Information", "Player joined", SessionId: "session-1", AccountId: "account-1", ClientName: "Test Player"));
        channel.Writer.TryComplete();

        await using var app = CreateApp(channel.Reader);
        await app.StartAsync();
        var endpoint = FindEndpoint(app.Services, "/api/events/stream");
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/events/stream";
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("text/event-stream", context.Response.Headers.ContentType.ToString());
        Assert.Contains("event: PlayerJoined", body);
        Assert.Contains("Test Player", body);
    }

    private static WebApplication CreateApp(ChannelReader<WindroseEvent>? events = null, bool redactSensitiveMetadata = false, string? serverFilesPath = null, ChannelCheevosPollReadback? channelCheevosReadback = null, bool pluginBridgeDevExecutionEnabled = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<IOptions<WindroseStateOptions>>(_ => Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions
        {
            RedactSensitiveMetadata = redactSensitiveMetadata,
            ServerFilesPath = serverFilesPath ?? "/server-files",
            PluginBridgeDevExecutionEnabled = pluginBridgeDevExecutionEnabled
        }));
        builder.Services.AddSingleton<IWindroseStateStore>(_ => new StubStateStore(events));
        builder.Services.AddSingleton<IChannelCheevosStatePoller>(_ => new StubChannelCheevosStatePoller(channelCheevosReadback));
        var app = builder.Build();
        app.MapWindroseStateHub();
        app.MapWindroseStateEndpoints();
        app.MapWindrosePluginBridgeEndpoints();
        app.MapChannelCheevosStateEndpoints();
        return app;
    }

    private static async Task<string> InvokeGetAsync(WebApplication app, string path)
    {
        var endpoint = FindEndpoint(app.Services, path);
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static async Task<string> InvokePostJsonAsync(WebApplication app, string path, string json)
    {
        var endpoint = FindEndpoint(app.Services, path);
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = path;
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static async Task<string> InvokeGetWithRouteValueAsync(WebApplication app, string routePattern, string path, string routeKey, string routeValue)
    {
        var endpoint = FindEndpoint(app.Services, routePattern);
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.Request.RouteValues[routeKey] = routeValue;
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static RouteEndpoint FindEndpoint(IServiceProvider services, string route)
    {
        var dataSource = services.GetRequiredService<EndpointDataSource>();
        return Assert.Single(dataSource.Endpoints.OfType<RouteEndpoint>(), endpoint => string.Equals(endpoint.RoutePattern.RawText, route, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class StubChannelCheevosStatePoller(ChannelCheevosPollReadback? readback) : IChannelCheevosStatePoller
    {
        public Task<ChannelCheevosPollReadback> PollAsync(CancellationToken cancellationToken = default) => Task.FromResult(readback ?? new ChannelCheevosPollReadback
        {
            Enabled = false,
            Configured = false,
            Target = "prod",
            Status = "disabled",
            Message = "ChannelCheevos polling is disabled.",
            ObservedAtUtc = DateTimeOffset.Parse("2026-06-05T00:00:00Z")
        });
    }

    private sealed class StubStateStore(ChannelReader<WindroseEvent>? events = null) : IWindroseStateStore
    {
        public WindroseServerState GetState() => new()
        {
            LogAvailable = true,
            ParserStatus = "Running",
            IsReady = true,
            CurrentIslandId = "8D23C893C50A4DAF6390E4E698FC5C8E",
            ServerName = "Windrose Test",
            InviteCode = "dbcdevs",
            Players =
            [
                new PlayerConnectionState
                {
                    Key = "session-1",
                    SessionId = "session-1",
                    AccountId = "account-1",
                    ClientName = "Test Player",
                    FirstSeen = DateTimeOffset.Parse("2026-05-21T20:00:00Z"),
                    LastSeen = DateTimeOffset.Parse("2026-05-21T20:01:00Z"),
                    Phase = "connected"
                }
            ],
            RecentEvents =
            [
                new WindroseEvent(DateTimeOffset.Parse("2026-05-21T20:00:00Z"), "PlayerJoined", "Information", "Player joined", SessionId: "session-1", AccountId: "account-1", ClientName: "Test Player")
            ],
            RecentHistory =
            [
                new WindroseTimelineEntry
                {
                    Timestamp = DateTimeOffset.Parse("2026-05-21T19:59:00Z"),
                    Category = "State",
                    Type = "SaveMetadataUpdated",
                    Severity = "Information",
                    Message = "Latest save metadata refreshed",
                    Source = "save-metadata",
                    Properties = new Dictionary<string, string>
                    {
                        ["worldName"] = "Test World"
                    }
                },
                new WindroseTimelineEntry
                {
                    Timestamp = DateTimeOffset.Parse("2026-05-21T20:00:00Z"),
                    Category = "Event",
                    Type = "PlayerJoined",
                    Severity = "Information",
                    Message = "Player joined",
                    SessionId = "session-1",
                    AccountId = "account-1",
                    ClientName = "Test Player",
                    Source = "log"
                }
            ],
            Save = new SaveMetadata
            {
                ActiveIslandId = "8D23C893C50A4DAF6390E4E698FC5C8E",
                WorldIslandId = "8D23C893C50A4DAF6390E4E698FC5C8E",
                WorldName = "Test World",
                WorldPresetType = "Custom",
                CheckpointContainerFormat = "RocksDB block-based SST",
                CheckpointExtractedPath = "/tmp/checkpoint",
                CheckpointEntries =
                [
                    new CheckpointEntrySummary
                    {
                        Path = "a",
                        SizeBytes = 1,
                        Kind = "sst",
                        Markers = ["ShipId"],
                        ReadableTokens = ["Blocks", "DataKey"]
                    }
                ],
                ObservedFamilies =
                [
                    new ObservedFamilySummary
                    {
                        Name = "island",
                        Status = "present",
                        Notes = "Island and world geometry families are visible in live checkpoint SSTs.",
                        Evidence = ["CommonIsland"]
                    },
                    new ObservedFamilySummary
                    {
                        Name = "actor",
                        Status = "present",
                        Notes = "Actor family markers are visible in live checkpoint SSTs.",
                        Evidence = ["Actor_InteractedPoiIds"]
                    },
                    new ObservedFamilySummary
                    {
                        Name = "player-in-world-metadata",
                        Status = "metadata-only",
                        Notes = "Player-in-world family names are visible in RocksDB metadata, but no standalone player document has been decoded yet.",
                        Evidence = ["R5BLPlayerInWorld", "R5BLPlayer"]
                    },
                    new ObservedFamilySummary
                    {
                        Name = "ship-reference",
                        Status = "reference-only",
                        Notes = "ShipId appears in live SST payloads, but no standalone R5BLShip document has been found in the current snapshot set.",
                        Evidence = ["ShipId"]
                    },
                    new ObservedFamilySummary
                    {
                        Name = "ship-document",
                        Status = "not-observed",
                        Notes = "No standalone R5BLShip document is present in the current live save tree.",
                        Evidence = ["R5BLShip"]
                    }
                ],
                ServerDescription = new ServerDescriptionMetadata
                {
                    InviteCode = "dbcdevs",
                    ServerName = "Windrose Test",
                    WorldIslandId = "8D23C893C50A4DAF6390E4E698FC5C8E",
                    MaxPlayerCount = 10,
                    P2pProxyAddress = "127.0.0.1",
                    DirectConnectionProxyAddress = "0.0.0.0",
                    UseDirectConnection = false,
                    DirectConnectionServerPort = 7777,
                    UserSelectedRegion = "EU",
                    DirectConnectionServerAddress = ""
                }
            }
        };

        public void SetLogAvailable(bool available, string? error = null) { }
        public void Apply(WindroseEvent evt) { }
        public void UpdateSaveMetadata(SaveMetadata save) { }
        public ChannelReader<WindroseEvent> Subscribe(CancellationToken cancellationToken) => events ?? throw new NotSupportedException();
        public ChannelReader<WindroseStateChange> SubscribeStateChanges(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
