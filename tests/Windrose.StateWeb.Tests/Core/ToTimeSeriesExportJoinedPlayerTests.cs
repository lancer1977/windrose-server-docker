using System.Collections.Generic;
using Windrose.StateWeb.Core.Contracts;
using Windrose.StateWeb.Core.Extensions;
using Windrose.StateWeb.Core.Models;

namespace Windrose.StateWeb.Tests.Core;

public sealed class ToTimeSeriesExportJoinedPlayerTests
{
    [Fact]
    public void ToTimeSeriesExport_IncrementsConnectedPlayersOnJoin()
    {
        var history = new List<WindroseTimelineEntry>
        {
            new() { Category = "Event", Type = "PlayerJoined", Timestamp = new DateTimeOffset(2026, 3, 1, 8, 30, 1, TimeSpan.Zero) },
            new() { Category = "Event", Type = "PlayerJoined", Timestamp = new DateTimeOffset(2026, 3, 1, 8, 30, 2, TimeSpan.Zero) }
        };

        var window = new WindroseTimeSeriesWindow
        {
            ConnectedPlayerCount = 0,
            EventCount = 0,
            History = history
        };

        var exported = window.ToTimeSeriesExport(new DateTimeOffset(2026, 3, 1, 8, 30, 3, TimeSpan.Zero));

        Assert.Equal(2, exported.SampleCount);
        Assert.Equal(1, exported.Points[0].ConnectedPlayerCount);
        Assert.Equal(2, exported.Points[1].ConnectedPlayerCount);
        Assert.Equal(1, exported.Points[0].EventCount);
        Assert.Equal(2, exported.Points[1].EventCount);
    }
}

