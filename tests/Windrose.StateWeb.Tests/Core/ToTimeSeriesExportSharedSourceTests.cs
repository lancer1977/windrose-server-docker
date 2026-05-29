using Windrose.StateWeb.Core.Contracts;
using Windrose.StateWeb.Core.Extensions;
using Windrose.StateWeb.Core.Models;

namespace Windrose.StateWeb.Tests.Core;

public sealed class ToTimeSeriesExportSharedSourceTests
{
    [Fact]
    public void ToTimeSeriesExport_WorksFromAnySharedSourceImplementation()
    {
        var at = new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);
        var history = new[]
        {
            new WindroseTimelineEntry
            {
                Timestamp = at.AddSeconds(1),
                Category = "Event",
                Type = "PlayerJoined"
            },
            new WindroseTimelineEntry
            {
                Timestamp = at.AddSeconds(2),
                Category = "Event",
                Type = "LogAvailabilityChanged",
                Properties = new Dictionary<string, string>
                {
                    ["available"] = "true"
                }
            }
        };

        var source = new WindroseTimeSeriesContext
        {
            History = history,
            LogAvailable = false,
            CurrentIslandId = "island-7",
            ConnectedPlayerCount = 0,
            EventCount = 0
        };

        var export = source.ToTimeSeriesExport(at);

        Assert.Equal(2, export.SampleCount);
        Assert.Equal(at.AddSeconds(1), export.WindowStart);
        Assert.Equal(at.AddSeconds(2), export.WindowEnd);
        Assert.Equal(1, export.Points[0].ConnectedPlayerCount);
        Assert.True(export.Points[1].LogAvailable);
        Assert.Equal("island-7", export.Points[0].CurrentIslandId);
        Assert.Equal("island-7", export.Points[1].CurrentIslandId);
    }
}
