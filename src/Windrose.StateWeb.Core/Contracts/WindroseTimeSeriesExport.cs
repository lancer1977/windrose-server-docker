namespace Windrose.StateWeb.Core.Contracts;

public sealed record WindroseTimeSeriesExport
{
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? WindowStart { get; init; }
    public DateTimeOffset? WindowEnd { get; init; }
    public int SampleCount { get; init; }
    public IReadOnlyList<WindroseTimeSeriesPoint> Points { get; init; } = [];
}
