using Windrose.StateWeb.Core.Models;

namespace Windrose.StateWeb.Tests.Core;

public sealed class WindroseOverlaySnapshotContextDefaultsTests
{
    [Fact]
    public void WindroseOverlaySnapshotContext_UsesExpectedDefaults_WhenNotConfigured()
    {
        var context = new WindroseOverlaySnapshotContext();

        Assert.False(context.LogAvailable);
        Assert.Equal("Starting", context.ParserStatus);
        Assert.Null(context.ServerName);
        Assert.Equal(0, context.ConnectedPlayerCount);
        Assert.Null(context.LatestBackupAge);
        Assert.Empty(context.Highlights);
    }

    [Fact]
    public void WindroseOverlaySnapshotContext_PreservesConfiguredValues()
    {
        var context = new WindroseOverlaySnapshotContext
        {
            LogAvailable = true,
            ParserStatus = "Running",
            ServerName = "server-1",
            CurrentIslandId = "island-1",
            WorldName = "Arcadia",
            WorldPresetType = "Normal",
            ConnectedPlayerCount = 4,
            TotalPlayerCount = 6,
            RecentEventCount = 7,
            RecentHistoryCount = 9,
            ObservedFamilyCount = 2,
            HasStandaloneShipDocument = true,
            LatestBackupAge = "1m ago",
            LatestBackupPath = "/tmp/save",
            Highlights = ["a", "b"]
        };

        Assert.True(context.LogAvailable);
        Assert.Equal("Running", context.ParserStatus);
        Assert.Equal("server-1", context.ServerName);
        Assert.Equal("island-1", context.CurrentIslandId);
        Assert.Equal("Arcadia", context.WorldName);
        Assert.Equal("Normal", context.WorldPresetType);
        Assert.Equal(4, context.ConnectedPlayerCount);
        Assert.Equal(6, context.TotalPlayerCount);
        Assert.Equal(7, context.RecentEventCount);
        Assert.Equal(9, context.RecentHistoryCount);
        Assert.Equal(2, context.ObservedFamilyCount);
        Assert.True(context.HasStandaloneShipDocument);
        Assert.Equal("1m ago", context.LatestBackupAge);
        Assert.Equal("/tmp/save", context.LatestBackupPath);
        Assert.Equal(new[] { "a", "b" }, context.Highlights);
    }
}
