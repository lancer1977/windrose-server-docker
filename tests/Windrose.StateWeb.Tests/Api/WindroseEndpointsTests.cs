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
        Assert.Contains("\"windrose.spawn.dodo_swarm\"", body);
        Assert.Contains("\"mode\":\"dry-run-only\"", body);
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
            {"pluginId":"windrose-sidecar-bridge","status":"started","startedAt":"2026-06-04T00:00:00Z","sidecarUrl":"http://127.0.0.1:8781","mode":"dry-run-only","message":"test heartbeat"}
            """);

        try
        {
            await using var app = CreateApp(serverFilesPath: tempRoot);
            await app.StartAsync();
            var body = await InvokeGetAsync(app, "/api/plugin/status");

            Assert.Contains("\"connected\":true", body);
            Assert.Contains("\"status\":\"started\"", body);
            Assert.Contains("\"mode\":\"dry-run-only\"", body);
            Assert.Contains("test heartbeat", body);
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

    private static WebApplication CreateApp(ChannelReader<WindroseEvent>? events = null, bool redactSensitiveMetadata = false, string? serverFilesPath = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<IOptions<WindroseStateOptions>>(_ => Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions
        {
            RedactSensitiveMetadata = redactSensitiveMetadata,
            ServerFilesPath = serverFilesPath ?? "/server-files"
        }));
        builder.Services.AddSingleton<IWindroseStateStore>(_ => new StubStateStore(events));
        var app = builder.Build();
        app.MapWindroseStateHub();
        app.MapWindroseStateEndpoints();
        app.MapWindrosePluginBridgeEndpoints();
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

    private static RouteEndpoint FindEndpoint(IServiceProvider services, string route)
    {
        var dataSource = services.GetRequiredService<EndpointDataSource>();
        return Assert.Single(dataSource.Endpoints.OfType<RouteEndpoint>(), endpoint => string.Equals(endpoint.RoutePattern.RawText, route, StringComparison.OrdinalIgnoreCase));
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
