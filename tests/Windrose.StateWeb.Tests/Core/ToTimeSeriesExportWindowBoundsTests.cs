using System.Collections.Generic;
using Windrose.StateWeb.Core.Contracts;
using Windrose.StateWeb.Core.Extensions;
using Windrose.StateWeb.Core.Models;

namespace Windrose.StateWeb.Tests.Core;

public sealed class ToTimeSeriesExportWindowBoundsTests
{
    [Fact]
    public void ToTimeSeriesExport_UsesFirstAndLastPointForWindowBounds()
    {
        var history = new List<WindroseTimelineEntry>
        {
            new() { Timestamp = new DateTimeOffset(2026, 3, 5, 0, 0, 2, TimeSpan.Zero), Category = "State", Type = "LogAvailabilityChanged", Properties = new Dictionary<string, string> { ["available"] = "true" } },
            new() { Timestamp = new DateTimeOffset(2026, 3, 5, 0, 0, 5, TimeSpan.Zero), Category = "Event", Type = "PlayerJoined" },
            new() { Timestamp = new DateTimeOffset(2026, 3, 5, 0, 0, 9, TimeSpan.Zero), Category = "Event", Type = "PlayerLeft" }
        };

        var window = new WindroseTimeSeriesWindow { History = history };

        var exported = window.ToTimeSeriesExport(new DateTimeOffset(2026, 3, 5, 0, 1, 0, TimeSpan.Zero));

        Assert.Equal(new DateTimeOffset(2026, 3, 5, 0, 0, 2, TimeSpan.Zero), exported.WindowStart);
        Assert.Equal(new DateTimeOffset(2026, 3, 5, 0, 0, 9, TimeSpan.Zero), exported.WindowEnd);
        Assert.Equal(3, exported.SampleCount);
        Assert.True(exported.Points[2].ConnectedPlayerCount >= 0);
    }
}

