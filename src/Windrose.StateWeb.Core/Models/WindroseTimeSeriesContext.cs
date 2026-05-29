using Windrose.StateWeb.Core.Abstractions;
using Windrose.StateWeb.Core.Contracts;

namespace Windrose.StateWeb.Core.Models;

public sealed record WindroseTimeSeriesContext : IWindroseTimeSeriesSource
{
    public IReadOnlyList<WindroseTimelineEntry> History { get; init; } = [];
    public bool LogAvailable { get; init; }
    public string? CurrentIslandId { get; init; }
    public int ConnectedPlayerCount { get; init; }
    public int EventCount { get; init; }
}
