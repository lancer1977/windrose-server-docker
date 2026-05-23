namespace Windrose.StateWeb.Domain;

public sealed record SaveCollectionSummary
{
    public string Name { get; init; } = "";
    public int Count { get; init; }
    public long TotalBytes { get; init; }
}
