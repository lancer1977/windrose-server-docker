using System.Collections.Generic;
using Windrose.StateWeb.Core.Contracts;
using Windrose.StateWeb.Core.Extensions;
using Windrose.StateWeb.Core.Models;

namespace Windrose.StateWeb.Tests.Core;

public sealed class ToTimeSeriesExportDisconnectedPlayerTests
{
    [Fact]
    public void ToTimeSeriesExport_DecrementsConnectedPlayersAndClampsAtZero()
    {
        var history = new List<WindroseTimelineEntry>
        {
            new() { Category = "Event", Type = "PlayerDisconnected", Timestamp = new DateTimeOffset(2026, 3, 1, 9, 0, 1, TimeSpan.Zero) },
            new() { Category = "Event", Type = "PlayerLeft", Timestamp = new DateTimeOffset(2026, 3, 1, 9, 0, 2, TimeSpan.Zero) }
        };

        var window = new WindroseTimeSeriesWindow
        {
            ConnectedPlayerCount = 0,
            EventCount = 10,
            History = history
        };

        var exported = window.ToTimeSeriesExport(new DateTimeOffset(2026, 3, 1, 9, 0, 3, TimeSpan.Zero));

        Assert.Equal(2, exported.SampleCount);
        Assert.Equal(0, exported.Points[0].ConnectedPlayerCount);
        Assert.Equal(0, exported.Points[1].ConnectedPlayerCount);
        Assert.Equal(12, exported.Points[1].EventCount);
    }
}
