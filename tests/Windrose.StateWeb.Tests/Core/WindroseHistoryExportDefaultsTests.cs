using Windrose.StateWeb.Core.Contracts;

namespace Windrose.StateWeb.Tests.Core;

public sealed class WindroseHistoryExportDefaultsTests
{
    [Fact]
    public void WindroseHistoryExport_UsesExpectedDefaults_WhenNotConfigured()
    {
        var export = new WindroseHistoryExport();

        Assert.NotEqual(default, export.GeneratedAt);
        Assert.Equal(0, export.EntryCount);
        Assert.Empty(export.Entries);
    }

    [Fact]
    public void WindroseHistoryExport_PreservesConfiguredValues()
    {
        var generatedAt = new DateTimeOffset(2026, 5, 22, 12, 0, 0, TimeSpan.Zero);
        var entry = new WindroseTimelineEntry { Category = "Event", Type = "PlayerJoined" };
        var export = new WindroseHistoryExport
        {
            GeneratedAt = generatedAt,
            EntryCount = 1,
            Entries = [entry]
        };

        Assert.Equal(generatedAt, export.GeneratedAt);
        Assert.Equal(1, export.EntryCount);
        Assert.Same(entry, export.Entries[0]);
    }
}
