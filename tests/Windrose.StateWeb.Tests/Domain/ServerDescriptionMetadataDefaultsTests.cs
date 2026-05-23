using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Tests.Domain;

public sealed class ServerDescriptionMetadataDefaultsTests
{
    [Fact]
    public void ServerDescriptionMetadata_UsesDefaultValues_WhenNotConfigured()
    {
        var metadata = new ServerDescriptionMetadata();

        Assert.Null(metadata.SourcePath);
        Assert.Null(metadata.LastModified);
        Assert.Null(metadata.PersistentServerId);
        Assert.Null(metadata.InviteCode);
        Assert.Null(metadata.IsPasswordProtected);
        Assert.Null(metadata.ServerName);
        Assert.Null(metadata.WorldIslandId);
        Assert.Null(metadata.MaxPlayerCount);
        Assert.Null(metadata.P2pProxyAddress);
        Assert.Null(metadata.DirectConnectionProxyAddress);
        Assert.Null(metadata.UseDirectConnection);
        Assert.Null(metadata.DirectConnectionServerPort);
        Assert.Null(metadata.UserSelectedRegion);
        Assert.Null(metadata.DirectConnectionServerAddress);
        Assert.Null(metadata.Source);
    }
}
