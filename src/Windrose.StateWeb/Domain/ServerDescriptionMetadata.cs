namespace Windrose.StateWeb.Domain;

public sealed record ServerDescriptionMetadata
{
    public string? SourcePath { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public string? PersistentServerId { get; init; }
    public string? InviteCode { get; init; }
    public bool? IsPasswordProtected { get; init; }
    public string? ServerName { get; init; }
    public string? WorldIslandId { get; init; }
    public int? MaxPlayerCount { get; init; }
    public string? P2pProxyAddress { get; init; }
    public string? DirectConnectionProxyAddress { get; init; }
    public bool? UseDirectConnection { get; init; }
    public int? DirectConnectionServerPort { get; init; }
    public string? UserSelectedRegion { get; init; }
    public string? DirectConnectionServerAddress { get; init; }
    public string? Source { get; init; }
}
