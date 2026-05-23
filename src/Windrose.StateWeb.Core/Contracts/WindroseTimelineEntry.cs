namespace Windrose.StateWeb.Core.Contracts;

public sealed record WindroseTimelineEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string Category { get; init; } = "";
    public string Type { get; init; } = "";
    public string Severity { get; init; } = "";
    public string Message { get; init; } = "";
    public string? SessionId { get; init; }
    public string? AccountId { get; init; }
    public string? ClientName { get; init; }
    public string? IslandId { get; init; }
    public string? Source { get; init; }
    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
}
