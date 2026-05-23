using System.Collections.Generic;
using Windrose.StateWeb.Core.Contracts;
using Windrose.StateWeb.Core.Extensions;
using Windrose.StateWeb.Core.Models;

namespace Windrose.StateWeb.Tests.Core;

public sealed class ToTimeSeriesExportLogAvailabilityToggleTests
{
    [Fact]
    public void ToTimeSeriesExport_TracksLogAvailabilityChanges()
    {
        var history = new List<WindroseTimelineEntry>
        {
            new()
            {
                Category = "State",
                Type = "LogAvailabilityChanged",
                Timestamp = new DateTimeOffset(2026, 3, 2, 10, 0, 1, TimeSpan.Zero),
                Properties = new Dictionary<string, string> { ["available"] = "false" }
            },
            new()
            {
                Category = "State",
                Type = "LogAvailabilityChanged",
                Timestamp = new DateTimeOffset(2026, 3, 2, 10, 0, 2, TimeSpan.Zero),
                Properties = new Dictionary<string, string> { ["available"] = "true" }
            }
        };

        var window = new WindroseTimeSeriesWindow
        {
            LogAvailable = true,
            History = history
        };

        var exported = window.ToTimeSeriesExport(new DateTimeOffset(2026, 3, 2, 10, 0, 3, TimeSpan.Zero));

        Assert.False(exported.Points[0].LogAvailable);
        Assert.True(exported.Points[1].LogAvailable);
    }
}

