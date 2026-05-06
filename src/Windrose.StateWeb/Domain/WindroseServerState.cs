namespace Windrose.StateWeb.Domain;

public sealed record WindroseServerState
{
    public bool LogAvailable { get; init; }
    public DateTimeOffset? LastLogRead { get; init; }
    public DateTimeOffset? LastEventAt { get; init; }
    public string? CurrentIslandId { get; init; }
    public string? ServerName { get; init; }
    public string? InviteCode { get; init; }
    public int? MaxPlayers { get; init; }
    public bool? UseDirectConnection { get; init; }
    public int? DirectConnectionServerPort { get; init; }
    public bool IsReady { get; init; }
    public string ParserStatus { get; init; } = "Starting";
    public string? ParserError { get; init; }
    public SaveMetadata Save { get; init; } = new();
    public IReadOnlyList<PlayerConnectionState> Players { get; init; } = [];
    public IReadOnlyList<WindroseEvent> RecentEvents { get; init; } = [];
    public IReadOnlyList<WindroseEvent> RecentWarnings { get; init; } = [];
}
