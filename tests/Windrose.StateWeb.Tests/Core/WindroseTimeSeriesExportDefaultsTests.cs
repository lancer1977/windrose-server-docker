using Windrose.StateWeb.Core.Contracts;

namespace Windrose.StateWeb.Tests.Core;

public sealed class WindroseTimeSeriesExportDefaultsTests
{
    [Fact]
    public void WindroseTimeSeriesExport_UsesExpectedDefaults_WhenNotConfigured()
    {
        var export = new WindroseTimeSeriesExport();

        Assert.NotEqual(default, export.GeneratedAt);
        Assert.Null(export.WindowStart);
        Assert.Null(export.WindowEnd);
        Assert.Equal(0, export.SampleCount);
        Assert.Empty(export.Points);
    }

    [Fact]
    public void WindroseTimeSeriesExport_PreservesConfiguredValues()
    {
        var at = new DateTimeOffset(2026, 5, 22, 12, 0, 0, TimeSpan.Zero);
        var point = new WindroseTimeSeriesPoint { Timestamp = at, LogAvailable = true, ConnectedPlayerCount = 2 };
        var export = new WindroseTimeSeriesExport
        {
            GeneratedAt = at,
            WindowStart = at,
            WindowEnd = at,
            SampleCount = 1,
            Points = [point]
        };

        Assert.Equal(at, export.GeneratedAt);
        Assert.Equal(at, export.WindowStart);
        Assert.Equal(at, export.WindowEnd);
        Assert.Equal(1, export.SampleCount);
        Assert.Same(point, export.Points[0]);
    }
}
