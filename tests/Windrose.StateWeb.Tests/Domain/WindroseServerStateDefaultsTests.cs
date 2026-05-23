using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Tests.Domain;

public sealed class WindroseServerStateDefaultsTests
{
    [Fact]
    public void WindroseServerState_UsesDefaultCollections()
    {
        var state = new WindroseServerState();

        Assert.NotNull(state.Save);
        Assert.NotNull(state.Players);
        Assert.NotNull(state.RecentEvents);
        Assert.NotNull(state.RecentWarnings);
        Assert.NotNull(state.RecentHistory);
        Assert.Empty(state.Players);
        Assert.Empty(state.RecentEvents);
        Assert.Empty(state.RecentWarnings);
        Assert.Empty(state.RecentHistory);
        Assert.False(state.LogAvailable);
        Assert.Equal("Starting", state.ParserStatus);
        Assert.False(state.IsReady);
    }
}

