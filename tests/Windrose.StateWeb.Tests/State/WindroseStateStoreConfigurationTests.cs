using Microsoft.Extensions.Logging.Abstractions;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.Options;
using Windrose.StateWeb.State;

namespace Windrose.StateWeb.Tests.State;

public sealed class WindroseStateStoreConfigurationTests
{
    [Fact]
    public void AppliesServerSettingsObservedEventToRuntimeState()
    {
        var store = new WindroseStateStore(
            Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions
            {
                EventRetention = 102
            }),
            NullLogger<WindroseStateStore>.Instance);

        var observedSettings = new WindroseEvent(
            DateTimeOffset.UtcNow,
            "ServerSettingsObserved",
            "Information",
            "settings observed",
            Properties: new Dictionary<string, string>
            {
                ["WorldIslandId"] = "island-1",
                ["ServerName"] = "Windrose Unit Test",
                ["InviteCode"] = "inv-1",
                ["MaxPlayerCount"] = "12",
                ["UseDirectConnection"] = "true",
                ["DirectConnectionServerPort"] = "7777"
            });

        store.Apply(observedSettings);
        var state = store.GetState();

        Assert.Equal("island-1", state.CurrentIslandId);
        Assert.Equal("Windrose Unit Test", state.ServerName);
        Assert.Equal("inv-1", state.InviteCode);
        Assert.Equal(12, state.MaxPlayers);
        Assert.True(state.UseDirectConnection);
        Assert.Equal(7777, state.DirectConnectionServerPort);
        Assert.Equal("ServerSettingsObserved", state.RecentHistory.Single(entry => entry.Category == "Event").Type);
    }

    [Fact]
    public void KeepsConfiguredEventRetentionAtOrAboveMinimumAndDropsOldEvents()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions
        {
            EventRetention = 102
        });
        var store = new WindroseStateStore(options, NullLogger<WindroseStateStore>.Instance);

        for (var i = 0; i < 110; i++)
        {
            store.Apply(new WindroseEvent(
                DateTimeOffset.UtcNow.AddSeconds(i),
                $"PlayerJoined",
                "Information",
                $"player {i}",
                SessionId: $"session-{i}",
                ClientName: $"client-{i}"));
        }

        var state = store.GetState();

        Assert.Equal(102, state.RecentEvents.Count);
        Assert.Contains(state.RecentEvents, evt => evt.Message == "player 109");
        Assert.DoesNotContain(state.RecentEvents, evt => evt.Message == "player 0");
    }
}
