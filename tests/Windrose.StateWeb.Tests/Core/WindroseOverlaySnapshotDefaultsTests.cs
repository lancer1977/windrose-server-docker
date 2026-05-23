using Windrose.StateWeb.Core.Contracts;

namespace Windrose.StateWeb.Tests.Core;

public sealed class WindroseOverlaySnapshotDefaultsTests
{
    [Fact]
    public void WindroseOverlaySnapshot_UsesExpectedDefaults_WhenNotConfigured()
    {
        var snapshot = new WindroseOverlaySnapshot();

        Assert.NotEqual(default, snapshot.GeneratedAt);
        Assert.Equal("Starting", snapshot.ParserStatus);
        Assert.Null(snapshot.ServerName);
        Assert.Equal(0, snapshot.ConnectedPlayerCount);
        Assert.Empty(snapshot.Highlights);
    }

    [Fact]
    public void WindroseOverlaySnapshot_PreservesConfiguredValues()
    {
        var at = new DateTimeOffset(2026, 5, 22, 12, 0, 0, TimeSpan.Zero);
        var snapshot = new WindroseOverlaySnapshot
        {
            GeneratedAt = at,
            LogAvailable = true,
            ParserStatus = "Running",
            ServerName = "island",
            CurrentIslandId = "id-1",
            WorldName = "Arcadia",
            WorldPresetType = "Hardcore",
            ConnectedPlayerCount = 3
        };

        Assert.Equal(at, snapshot.GeneratedAt);
        Assert.True(snapshot.LogAvailable);
        Assert.Equal("Running", snapshot.ParserStatus);
        Assert.Equal("island", snapshot.ServerName);
        Assert.Equal("id-1", snapshot.CurrentIslandId);
        Assert.Equal("Arcadia", snapshot.WorldName);
        Assert.Equal("Hardcore", snapshot.WorldPresetType);
        Assert.Equal(3, snapshot.ConnectedPlayerCount);
    }
}
