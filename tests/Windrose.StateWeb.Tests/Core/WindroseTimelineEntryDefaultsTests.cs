using Windrose.StateWeb.Core.Contracts;

namespace Windrose.StateWeb.Tests.Core;

public sealed class WindroseTimelineEntryDefaultsTests
{
    [Fact]
    public void WindroseTimelineEntry_UsesExpectedDefaults_WhenNotConfigured()
    {
        var entry = new WindroseTimelineEntry();

        Assert.NotEqual(default, entry.Timestamp);
        Assert.Equal(string.Empty, entry.Category);
        Assert.Equal(string.Empty, entry.Type);
        Assert.Equal(string.Empty, entry.Severity);
        Assert.Equal(string.Empty, entry.Message);
        Assert.Null(entry.SessionId);
        Assert.Empty(entry.Properties);
    }
}
