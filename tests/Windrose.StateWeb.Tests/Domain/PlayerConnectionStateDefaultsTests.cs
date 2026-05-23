using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Tests.Domain;

public sealed class PlayerConnectionStateDefaultsTests
{
    [Fact]
    public void PlayerConnectionState_UsesDefaultValues_WhenNotConfigured()
    {
        var state = new PlayerConnectionState();

        Assert.Equal(string.Empty, state.Key);
        Assert.Equal("Observed", state.Phase);
        Assert.False(state.IsConnected);
        Assert.Equal(default, state.FirstSeen);
        Assert.Equal(default, state.LastSeen);
    }

    [Fact]
    public void PlayerConnectionState_PreservesConfiguredValues()
    {
        var seen = new DateTimeOffset(2026, 5, 22, 12, 0, 0, TimeSpan.Zero);

        var state = new PlayerConnectionState
        {
            Key = "session-1",
            SessionId = "sess-id",
            AccountId = "acct-id",
            ClientName = "hero",
            Phase = "Joining",
            IsConnected = true,
            FirstSeen = seen,
            LastSeen = seen.AddMinutes(2),
            DisconnectedAt = seen.AddMinutes(5),
            DisconnectReason = "timeout"
        };

        Assert.Equal("session-1", state.Key);
        Assert.Equal("sess-id", state.SessionId);
        Assert.Equal("acct-id", state.AccountId);
        Assert.Equal("hero", state.ClientName);
        Assert.Equal("Joining", state.Phase);
        Assert.True(state.IsConnected);
        Assert.Equal(seen, state.FirstSeen);
        Assert.Equal(seen.AddMinutes(2), state.LastSeen);
        Assert.Equal(seen.AddMinutes(5), state.DisconnectedAt);
        Assert.Equal("timeout", state.DisconnectReason);
    }
}
