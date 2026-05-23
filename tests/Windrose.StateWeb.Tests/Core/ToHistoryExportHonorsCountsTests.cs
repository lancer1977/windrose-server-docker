using Windrose.StateWeb.Core.Contracts;
using Windrose.StateWeb.Core.Abstractions;
using Windrose.StateWeb.Core.Extensions;

namespace Windrose.StateWeb.Tests.Core;

public sealed class ToHistoryExportHonorsCountsTests
{
    [Fact]
    public void ToHistoryExport_CountMatchesOrderedEntries()
    {
        var source = new WindroseHistorySourceStub(
            [
                new WindroseTimelineEntry { Category = "State", Type = "Started" },
                new WindroseTimelineEntry { Category = "Event", Type = "PlayerJoined" },
            ]);

        var result = source.ToHistoryExport(new DateTimeOffset(2026, 2, 2, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(2, result.EntryCount);
        Assert.Equal(result.Entries.Count, result.EntryCount);
    }

    private sealed class WindroseHistorySourceStub(IReadOnlyList<WindroseTimelineEntry> history) : IWindroseHistorySource
    {
        public IReadOnlyList<WindroseTimelineEntry> RecentHistory => history;
    }
}
