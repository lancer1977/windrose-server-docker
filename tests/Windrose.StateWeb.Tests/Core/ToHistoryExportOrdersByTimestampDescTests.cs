using Windrose.StateWeb.Core.Contracts;
using Windrose.StateWeb.Core.Abstractions;
using Windrose.StateWeb.Core.Extensions;
using Windrose.StateWeb.Core.Models;

namespace Windrose.StateWeb.Tests.Core;

public sealed class ToHistoryExportOrdersByTimestampDescTests
{
    [Fact]
    public void ToHistoryExport_OrdersRecentHistoryNewestToOldest()
    {
        var source = new WindroseHistorySourceStub(
            [
                new WindroseTimelineEntry { Timestamp = new DateTimeOffset(2026, 1, 1, 12, 0, 1, TimeSpan.Zero), Type = "Second", Category = "Event" },
                new WindroseTimelineEntry { Timestamp = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), Type = "First", Category = "Event" },
                new WindroseTimelineEntry { Timestamp = new DateTimeOffset(2026, 1, 1, 12, 0, 2, TimeSpan.Zero), Type = "Third", Category = "Event" }
            ]);

        var result = source.ToHistoryExport(new DateTimeOffset(2026, 1, 1, 12, 0, 3, TimeSpan.Zero));

        Assert.Equal(3, result.EntryCount);
        Assert.Equal("Third", result.Entries[0].Type);
        Assert.Equal("Second", result.Entries[1].Type);
        Assert.Equal("First", result.Entries[2].Type);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 12, 0, 3, TimeSpan.Zero), result.GeneratedAt);
    }

    private sealed class WindroseHistorySourceStub(IReadOnlyList<WindroseTimelineEntry> history) : IWindroseHistorySource
    {
        public IReadOnlyList<WindroseTimelineEntry> RecentHistory => history;
    }
}
