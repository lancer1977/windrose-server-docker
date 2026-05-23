namespace Windrose.StateWeb.Core.Contracts;

public sealed record WindroseTimeSeriesPoint
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public bool LogAvailable { get; init; }
    public string? CurrentIslandId { get; init; }
    public int ConnectedPlayerCount { get; init; }
    public int EventCount { get; init; }
    public int HistoryCount { get; init; }
}
