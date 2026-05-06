namespace Windrose.StateWeb.Domain;

public sealed record WindroseEvent(
    DateTimeOffset Timestamp,
    string Type,
    string Severity,
    string Message,
    string? SessionId = null,
    string? AccountId = null,
    string? ClientName = null,
    IReadOnlyDictionary<string, string>? Properties = null);
