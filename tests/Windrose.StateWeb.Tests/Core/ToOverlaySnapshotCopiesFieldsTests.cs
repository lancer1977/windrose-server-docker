using Windrose.StateWeb.Core.Contracts;
using Windrose.StateWeb.Core.Extensions;
using Windrose.StateWeb.Core.Models;

namespace Windrose.StateWeb.Tests.Core;

public sealed class ToOverlaySnapshotCopiesFieldsTests
{
    [Fact]
    public void ToOverlaySnapshot_CopiesAllMappedFields()
    {
        var source = new WindroseOverlaySnapshotContext
        {
            LogAvailable = true,
            ParserStatus = "Running",
            ServerName = "Windrose Test",
            CurrentIslandId = "island-1",
            WorldName = "World One",
            WorldPresetType = "Hardcore",
            ConnectedPlayerCount = 3,
            TotalPlayerCount = 8,
            RecentEventCount = 4,
            RecentHistoryCount = 7,
            ObservedFamilyCount = 1,
            HasStandaloneShipDocument = true,
            LatestBackupAge = "2m",
            LatestBackupPath = "/tmp/world",
            Highlights = ["first", "second"]
        };

        var snapshot = source.ToOverlaySnapshot(new DateTimeOffset(2026, 2, 2, 10, 0, 0, TimeSpan.Zero));

        Assert.True(snapshot.LogAvailable);
        Assert.Equal("Running", snapshot.ParserStatus);
        Assert.Equal("Windrose Test", snapshot.ServerName);
        Assert.Equal("island-1", snapshot.CurrentIslandId);
        Assert.Equal("World One", snapshot.WorldName);
        Assert.Equal(3, snapshot.ConnectedPlayerCount);
        Assert.Equal(8, snapshot.TotalPlayerCount);
        Assert.Equal(4, snapshot.RecentEventCount);
        Assert.Equal(7, snapshot.RecentHistoryCount);
        Assert.Equal(1, snapshot.ObservedFamilyCount);
        Assert.True(snapshot.HasStandaloneShipDocument);
        Assert.Equal("2m", snapshot.LatestBackupAge);
        Assert.Equal("/tmp/world", snapshot.LatestBackupPath);
        Assert.Equal("second", snapshot.Highlights[1]);
    }
}

