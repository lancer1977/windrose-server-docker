namespace Windrose.StateWeb.Domain;

public sealed record SaveRecordGraphReport
{
    public bool ReadOnly { get; init; } = true;
    public string? SourcePath { get; init; }
    public bool HasCrossLinkedIdentityAndPortableData { get; init; }
    public bool CanExportWithoutRekey { get; init; }
    public string Verdict { get; init; } = "inconclusive";
    public IReadOnlyList<string> RecordTypes { get; init; } = [];
    public IReadOnlyList<string> IdentityMarkers { get; init; } = [];
    public IReadOnlyList<string> CandidatePortableMarkers { get; init; } = [];
    public IReadOnlyList<string> ReferenceMarkers { get; init; } = [];
    public IReadOnlyList<string> CoLocatedEvidence { get; init; } = [];
    public IReadOnlyList<SaveRecordGraphEntrySummary> Entries { get; init; } = [];
}

public sealed record SaveRecordGraphEntrySummary
{
    public string Path { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Classification { get; init; } = "";
    public IReadOnlyList<string> RecordTypes { get; init; } = [];
    public IReadOnlyList<string> IdentityMarkers { get; init; } = [];
    public IReadOnlyList<string> CandidatePortableMarkers { get; init; } = [];
    public IReadOnlyList<string> ReferenceMarkers { get; init; } = [];
    public string? Notes { get; init; }
}
