namespace Windrose.StateWeb.Domain;

public sealed record CheckpointEntrySummary
{
    public string Path { get; init; } = "";
    public long SizeBytes { get; init; }
    public string Kind { get; init; } = "";
    public IReadOnlyList<string> Markers { get; init; } = [];
    public IReadOnlyList<string> ReadableTokens { get; init; } = [];
    public IReadOnlyList<string> RecordTypes { get; init; } = [];
    public IReadOnlyList<string> IdentityMarkers { get; init; } = [];
    public IReadOnlyList<string> CandidatePortableMarkers { get; init; } = [];
    public IReadOnlyList<string> ReferenceMarkers { get; init; } = [];
    public string Classification { get; init; } = "unclassified";
    public string? Notes { get; init; }
}
