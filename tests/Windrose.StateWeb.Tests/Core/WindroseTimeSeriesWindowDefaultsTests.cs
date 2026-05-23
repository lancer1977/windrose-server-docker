using Windrose.StateWeb.Core.Models;
using Windrose.StateWeb.Core.Contracts;

namespace Windrose.StateWeb.Tests.Core;

public sealed class WindroseTimeSeriesWindowDefaultsTests
{
    [Fact]
    public void WindroseTimeSeriesWindow_UsesExpectedDefaults_WhenNotConfigured()
    {
        var window = new WindroseTimeSeriesWindow();

        Assert.Empty(window.History);
        Assert.False(window.LogAvailable);
        Assert.Null(window.CurrentIslandId);
        Assert.Equal(0, window.ConnectedPlayerCount);
        Assert.Equal(0, window.EventCount);
    }

    [Fact]
    public void WindroseTimeSeriesWindow_PreservesConfiguredValues()
    {
        var history = new[] { new WindroseTimelineEntry { Category = "State", Type = "Start" } };
        var window = new WindroseTimeSeriesWindow
        {
            History = history,
            LogAvailable = true,
            CurrentIslandId = "island-1",
            ConnectedPlayerCount = 3,
            EventCount = 9
        };

        Assert.Equal(history.Length, window.History.Count);
        Assert.Equal(history[0].Type, window.History[0].Type);
        Assert.Equal(history[0].Category, window.History[0].Category);
        Assert.True(window.LogAvailable);
        Assert.Equal("island-1", window.CurrentIslandId);
        Assert.Equal(3, window.ConnectedPlayerCount);
        Assert.Equal(9, window.EventCount);
    }
}
