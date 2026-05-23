using Windrose.StateWeb.Core.Extensions;
using Windrose.StateWeb.Core.Models;

namespace Windrose.StateWeb.Tests.Core;

public sealed class ToTimeSeriesExportEmptyHistoryTests
{
    [Fact]
    public void ToTimeSeriesExport_ReturnsSingleSampleWhenNoHistory()
    {
        var window = new WindroseTimeSeriesWindow
        {
            LogAvailable = true,
            CurrentIslandId = "island-empty",
            ConnectedPlayerCount = 2,
            EventCount = 4
        };

        var exported = window.ToTimeSeriesExport(new DateTimeOffset(2026, 3, 1, 8, 30, 15, TimeSpan.Zero));

        Assert.Equal(1, exported.SampleCount);
        Assert.Single(exported.Points);
        Assert.Equal("island-empty", exported.Points[0].CurrentIslandId);
        Assert.Equal(2, exported.Points[0].ConnectedPlayerCount);
        Assert.Equal(4, exported.Points[0].EventCount);
        Assert.True(exported.Points[0].LogAvailable);
        Assert.Equal(exported.WindowStart, exported.Points[0].Timestamp);
        Assert.Equal(exported.WindowEnd, exported.Points[0].Timestamp);
    }
}

