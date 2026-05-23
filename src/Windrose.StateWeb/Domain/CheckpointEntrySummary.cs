namespace Windrose.StateWeb.Domain;

public sealed record CheckpointEntrySummary
{
    public string Path { get; init; } = "";
    public long SizeBytes { get; init; }
    public string Kind { get; init; } = "";
    public IReadOnlyList<string> Markers { get; init; } = [];
    public IReadOnlyList<string> ReadableTokens { get; init; } = [];
}
