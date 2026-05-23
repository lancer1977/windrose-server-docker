using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Tests.Domain;

public sealed class WindroseStateChangeDefaultsTests
{
    [Fact]
    public void WindroseStateChange_HasReasonableDefaults()
    {
        var change = new WindroseStateChange();

        Assert.Equal("", change.Kind);
        Assert.NotNull(change.State);
        Assert.Null(change.Event);
        Assert.Null(change.Notes);
        Assert.True(change.Timestamp <= DateTimeOffset.UtcNow);
    }
}

