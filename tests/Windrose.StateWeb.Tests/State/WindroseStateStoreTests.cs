using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.Options;
using Windrose.StateWeb.State;
using Windrose.StateWeb.Core.Contracts;

namespace Windrose.StateWeb.Tests.State;

public sealed class WindroseStateStoreTests
{
    [Fact]
    public async Task PublishesStateChangesWhenEventsAreApplied()
    {
        var store = new WindroseStateStore(Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions()), NullLogger<WindroseStateStore>.Instance);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var reader = store.SubscribeStateChanges(cts.Token);

        store.Apply(new WindroseEvent(DateTimeOffset.UtcNow, "PlayerJoined", "Information", "Player joined", SessionId: "session-1", AccountId: "account-1", ClientName: "Test Player"));

        var change = await reader.ReadAsync(cts.Token);

        Assert.Equal("EventApplied", change.Kind);
        Assert.NotNull(change.State);
        Assert.Single(change.State.Players);
        Assert.Equal("Test Player", change.State.Players[0].ClientName);
        Assert.Equal("session-1", change.Event?.SessionId);
    }

    [Fact]
    public void PersistsSnapshotWhenStateChanges()
    {
        var root = Path.Combine(Path.GetTempPath(), "windrose-state-tests", Guid.NewGuid().ToString("N"));
        var snapshotPath = Path.Combine(root, "current-state.json");

        var store = new WindroseStateStore(Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions
        {
            SnapshotPath = snapshotPath
        }), NullLogger<WindroseStateStore>.Instance);

        store.SetLogAvailable(true);
        store.Apply(new WindroseEvent(DateTimeOffset.UtcNow, "PlayerJoined", "Information", "Player joined", SessionId: "session-1", AccountId: "account-1", ClientName: "Test Player"));

        Assert.True(File.Exists(snapshotPath));

        var json = File.ReadAllText(snapshotPath);
        Assert.Contains("Test Player", json);
        Assert.Contains("PlayerJoined", json);

        Directory.Delete(root, true);
    }

    [Fact]
    public void RecordsRecentHistoryForStateChanges()
    {
        var store = new WindroseStateStore(Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions()), NullLogger<WindroseStateStore>.Instance);

        store.SetLogAvailable(true);
        store.Apply(new WindroseEvent(DateTimeOffset.UtcNow, "PlayerJoined", "Information", "Player joined", SessionId: "session-1", AccountId: "account-1", ClientName: "Test Player"));

        var state = store.GetState();

        Assert.Equal(2, state.RecentHistory.Count);
        Assert.Contains(state.RecentHistory, entry => entry.Type == "LogAvailabilityChanged");
        Assert.Contains(state.RecentHistory, entry => entry.Type == "PlayerJoined");
        Assert.All(state.RecentHistory, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Category)));
    }
}
