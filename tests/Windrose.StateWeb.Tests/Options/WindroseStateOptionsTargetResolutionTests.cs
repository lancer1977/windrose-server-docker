using Windrose.StateWeb.Options;

namespace Windrose.StateWeb.Tests.Options;

public sealed class WindroseStateOptionsTargetResolutionTests
{
    [Fact]
    public void ResolvesChannelCheevosWebKey_ForDebugTarget()
    {
        var options = new WindroseStateOptions
        {
            ChannelCheevosTarget = "DEBUG",
            ChannelCheevosWebKey = "fallback",
            ChannelCheevosWebKeyDebug = "debug-key"
        };

        Assert.Equal("debug-key", options.ResolveChannelCheevosWebKey());
        Assert.Equal("debug", options.ResolvedChannelCheevosTarget);
    }

    [Fact]
    public void ResolvesChannelCheevosWebKey_FallsBackToGenericForUnknownTarget()
    {
        var options = new WindroseStateOptions
        {
            ChannelCheevosTarget = "staging",
            ChannelCheevosWebKey = "fallback",
            ChannelCheevosWebKeyDebug = "debug-key"
        };

        Assert.Equal("fallback", options.ResolveChannelCheevosWebKey());
        Assert.Equal("staging", options.ResolvedChannelCheevosTarget);
    }

    [Fact]
    public void ResolvesChannelCheevosWebKey_FallsBackForWhitespaceTarget()
    {
        var options = new WindroseStateOptions
        {
            ChannelCheevosTarget = "   ",
            ChannelCheevosWebKey = "prod-key"
        };

        Assert.Equal("prod-key", options.ResolveChannelCheevosWebKey());
        Assert.Equal("prod", options.ResolvedChannelCheevosTarget);
    }
}

