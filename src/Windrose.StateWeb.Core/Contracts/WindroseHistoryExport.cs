namespace Windrose.StateWeb.Core.Contracts;

public sealed record WindroseHistoryExport
{
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
    public int EntryCount { get; init; }
    public IReadOnlyList<WindroseTimelineEntry> Entries { get; init; } = [];
}
