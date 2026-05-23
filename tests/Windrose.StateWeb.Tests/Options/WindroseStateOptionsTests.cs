using Windrose.StateWeb.Options;

namespace Windrose.StateWeb.Tests.Options;

public sealed class WindroseStateOptionsTests
{
    [Fact]
    public void ResolvesGenericHubAndWebKeyWhenTargetSpecificValuesAreMissing()
    {
        var options = new WindroseStateOptions
        {
            ChannelCheevosTarget = "prod",
            ChannelCheevosHubUrl = "https://example.test/generic",
            ChannelCheevosWebKey = "generic-key"
        };

        Assert.Equal("https://example.test/generic", options.ResolveChannelCheevosHubUrl());
        Assert.Equal("generic-key", options.ResolveChannelCheevosWebKey());
    }

    [Fact]
    public void ResolvesTargetSpecificHubAndWebKeyWhenConfigured()
    {
        var options = new WindroseStateOptions
        {
            ChannelCheevosTarget = "debug",
            ChannelCheevosHubUrl = "https://example.test/generic",
            ChannelCheevosHubUrlDebug = "https://example.test/debug",
            ChannelCheevosWebKey = "generic-key",
            ChannelCheevosWebKeyDebug = "debug-key"
        };

        Assert.Equal("https://example.test/debug", options.ResolveChannelCheevosHubUrl());
        Assert.Equal("debug-key", options.ResolveChannelCheevosWebKey());
    }
}
