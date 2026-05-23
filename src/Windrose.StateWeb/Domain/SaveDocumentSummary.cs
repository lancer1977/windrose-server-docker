namespace Windrose.StateWeb.Domain;

public sealed record SaveDocumentSummary
{
    public string Path { get; init; } = "";
    public string Kind { get; init; } = "";
    public long? SizeBytes { get; init; }
    public int ScalarPropertyCount { get; init; }
    public int ObjectCount { get; init; }
    public int ArrayCount { get; init; }
    public IReadOnlyDictionary<string, string> ScalarPreview { get; init; } = new Dictionary<string, string>();
    public string? Notes { get; init; }
}
