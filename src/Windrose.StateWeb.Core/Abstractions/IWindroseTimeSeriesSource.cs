using Windrose.StateWeb.Core.Contracts;

namespace Windrose.StateWeb.Core.Abstractions;

public interface IWindroseTimeSeriesSource
{
    IReadOnlyList<WindroseTimelineEntry> History { get; }
    bool LogAvailable { get; }
    string? CurrentIslandId { get; }
    int ConnectedPlayerCount { get; }
    int EventCount { get; }
}
