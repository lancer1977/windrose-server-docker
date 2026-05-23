namespace Windrose.StateWeb.Domain;

public sealed record ObservedFamilySummary
{
    public string Name { get; init; } = "";
    public string Status { get; init; } = "";
    public string Notes { get; init; } = "";
    public IReadOnlyList<string> Evidence { get; init; } = [];
}
