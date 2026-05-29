using Windrose.StateWeb.Core.Models;
using Windrose.StateWeb.Core.Contracts;

namespace Windrose.StateWeb.Tests.Core;

public sealed class WindroseTimeSeriesContextDefaultsTests
{
    [Fact]
    public void WindroseTimeSeriesContext_UsesExpectedDefaults_WhenNotConfigured()
    {
        var context = new WindroseTimeSeriesContext();

        Assert.Empty(context.History);
        Assert.False(context.LogAvailable);
        Assert.Null(context.CurrentIslandId);
        Assert.Equal(0, context.ConnectedPlayerCount);
        Assert.Equal(0, context.EventCount);
    }

    [Fact]
    public void WindroseTimeSeriesContext_PreservesConfiguredValues()
    {
        var history = new[] { new WindroseTimelineEntry { Category = "State", Type = "Start" } };
        var context = new WindroseTimeSeriesContext
        {
            History = history,
            LogAvailable = true,
            CurrentIslandId = "island-1",
            ConnectedPlayerCount = 3,
            EventCount = 9
        };

        Assert.Equal(history.Length, context.History.Count);
        Assert.Equal(history[0].Type, context.History[0].Type);
        Assert.Equal(history[0].Category, context.History[0].Category);
        Assert.True(context.LogAvailable);
        Assert.Equal("island-1", context.CurrentIslandId);
        Assert.Equal(3, context.ConnectedPlayerCount);
        Assert.Equal(9, context.EventCount);
    }
}
