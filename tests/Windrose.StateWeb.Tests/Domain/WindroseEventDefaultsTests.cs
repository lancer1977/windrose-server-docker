using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Tests.Domain;

public sealed class WindroseEventDefaultsTests
{
    [Fact]
    public void WindroseEvent_PreservesAllSuppliedValues()
    {
        var timestamp = new DateTimeOffset(2026, 3, 4, 13, 30, 0, TimeSpan.Zero);
        var item = new WindroseEvent(
            timestamp,
            "PlayerJoined",
            "Info",
            "A player joined",
            "session-1",
            "acct-1",
            "PlayerOne",
            new Dictionary<string, string> { ["foo"] = "bar" });

        Assert.Equal(timestamp, item.Timestamp);
        Assert.Equal("PlayerJoined", item.Type);
        Assert.Equal("Info", item.Severity);
        Assert.Equal("A player joined", item.Message);
        Assert.Equal("session-1", item.SessionId);
        Assert.Equal("acct-1", item.AccountId);
        Assert.Equal("PlayerOne", item.ClientName);
        Assert.Equal("bar", item.Properties?["foo"]);
    }
}

