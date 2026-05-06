using Windrose.StateWeb.Parsing;

namespace Windrose.StateWeb.Tests.Parsing;

public sealed class WindroseLogParserTests
{
    private readonly WindroseLogParser _parser = new();

    [Fact]
    public void ParsesServerReadyMarker()
    {
        var evt = _parser.ParseLine("[2026.05.04-21.28.14:837][ 62]R5LogCoopProxy: UR5CoopProxyServer::SetIsReadyForHostOwnerConnect Host server is ready for owner to connect.");

        Assert.NotNull(evt);
        Assert.Equal("ServerReady", evt.Type);
    }

    [Fact]
    public void ParsesServerInitializedIslandId()
    {
        var evt = _parser.ParseLine("[2026.05.04-21.28.12:638][  0]R5LogCoopProxy: UR5CoopProxyServer::Init Server initialized. CurrentIslandId F3B27E1F83434AF5A1BBA9B40E848A42");

        Assert.NotNull(evt);
        Assert.Equal("ServerInitialized", evt.Type);
        Assert.Equal("F3B27E1F83434AF5A1BBA9B40E848A42", evt.Properties?["islandId"]);
    }

    [Fact]
    public void ParsesPlayerJoin()
    {
        var evt = _parser.ParseLine("[2026.05.05-20.08.33:428][973]LogNet: Join request: /Game/Maps/Lobby/R5ServerLobby?BLPlayerSessionId=08bf811d1e58483ba5d0287c02718611?Name=linux-King-REDACTED?SplitscreenCount=1");

        Assert.NotNull(evt);
        Assert.Equal("PlayerJoined", evt.Type);
        Assert.Equal("08bf811d1e58483ba5d0287c02718611", evt.SessionId);
        Assert.Equal("linux-King-REDACTED", evt.ClientName);
    }

    [Fact]
    public void ParsesPlayerDisconnect()
    {
        var evt = _parser.ParseLine("[2026.05.05-20.15.35:447][756]R5LogCoopProxy: UR5CoopProxyServer::OnAccountDisconnected Account disconnected. Inform Cm. AccountId E65EF81A41AAF4BAC5BC979966825458. BLPlayerSessionId 08bf811d1e58483ba5d0287c02718611. DisconnectReason 'BL disconnected'. FarewellReason 'Go to lobby'");

        Assert.NotNull(evt);
        Assert.Equal("PlayerDisconnected", evt.Type);
        Assert.Equal("E65EF81A41AAF4BAC5BC979966825458", evt.AccountId);
        Assert.Equal("08bf811d1e58483ba5d0287c02718611", evt.SessionId);
        Assert.Equal("BL disconnected", evt.Properties?["disconnectReason"]);
    }

    [Fact]
    public void ParsesServerSettingsLine()
    {
        var evt = _parser.ParseLine("    \"ServerName\": \"Polyhydra Games\",");

        Assert.NotNull(evt);
        Assert.Equal("ServerSettingsObserved", evt.Type);
        Assert.Equal("Polyhydra Games", evt.Properties?["ServerName"]);
    }
}
