namespace Windrose.StateWeb.Domain;

public sealed record WindroseStateChange
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string Kind { get; init; } = "";
    public WindroseServerState State { get; init; } = new();
    public WindroseEvent? Event { get; init; }
    public string? Notes { get; init; }
}
