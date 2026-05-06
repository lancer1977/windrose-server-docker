namespace Windrose.StateWeb.Domain;

public sealed record PlayerConnectionState
{
    public string Key { get; init; } = "";
    public string? SessionId { get; init; }
    public string? AccountId { get; init; }
    public string? ClientName { get; init; }
    public string Phase { get; init; } = "Observed";
    public bool IsConnected { get; init; }
    public DateTimeOffset FirstSeen { get; init; }
    public DateTimeOffset LastSeen { get; init; }
    public DateTimeOffset? DisconnectedAt { get; init; }
    public string? DisconnectReason { get; init; }
}
