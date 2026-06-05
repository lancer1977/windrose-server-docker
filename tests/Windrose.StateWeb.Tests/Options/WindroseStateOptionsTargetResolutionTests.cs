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

    [Fact]
    public void ResolvesChannelCheevosStateUrl_FromExplicitTargetUrl()
    {
        var options = new WindroseStateOptions
        {
            ChannelCheevosTarget = "dev",
            ChannelCheevosStateUrl = "https://prod.example/api/windrose/state",
            ChannelCheevosStateUrlDev = "https://dev.example/api/windrose/state"
        };

        Assert.Equal("https://dev.example/api/windrose/state", options.ResolveChannelCheevosStateUrl());
    }

    [Fact]
    public void ResolvesChannelCheevosStateUrl_FromBaseUrlWithoutExposingWebKey()
    {
        var options = new WindroseStateOptions
        {
            ChannelCheevosBaseUrl = "https://channel-cheevos.example/",
            ChannelCheevosWebKey = "secret-key"
        };

        Assert.Equal("https://channel-cheevos.example/api/windrose/state", options.ResolveChannelCheevosStateUrl());
        Assert.DoesNotContain("secret-key", options.ResolveChannelCheevosStateUrl(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolvesChannelCheevosStateUrl_FromHubOriginWhenBaseUrlMissing()
    {
        var options = new WindroseStateOptions
        {
            ChannelCheevosHubUrl = "https://channel-cheevos.example/hubs/windrose"
        };

        Assert.Equal("https://channel-cheevos.example/api/windrose/state", options.ResolveChannelCheevosStateUrl());
    }
}

