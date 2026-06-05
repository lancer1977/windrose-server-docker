using System.Text.Json.Serialization;

namespace Windrose.StateWeb.Services;

public sealed class ChannelCheevosStateSnapshot
{
    public string ChannelName { get; init; } = string.Empty;
    public DateTimeOffset GeneratedAtUtc { get; init; }
    public bool IsInitialized { get; init; }
    public IReadOnlyList<string> ConnectedFeatures { get; init; } = [];
    public ChannelCheevosStreamSnapshot? Stream { get; init; }
}

public sealed class ChannelCheevosStreamSnapshot
{
    public string StreamId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public bool Finalized { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public TimeSpan Duration { get; init; }
    public int SubscriberCount { get; init; }
    public int ChatterCount { get; init; }
    public int DonatorCount { get; init; }
    public int RaiderCount { get; init; }
    public int EnderCount { get; init; }
}

public sealed class ChannelCheevosPollReadback
{
    public bool Enabled { get; init; }
    public bool Configured { get; init; }
    public string Target { get; init; } = "prod";
    public string Endpoint { get; init; } = string.Empty;
    public string Status { get; init; } = "disabled";
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset ObservedAtUtc { get; init; }
    public ChannelCheevosStateSnapshot? State { get; init; }

    [JsonIgnore]
    public string? RawError { get; init; }
}

public interface IChannelCheevosStatePoller
{
    Task<ChannelCheevosPollReadback> PollAsync(CancellationToken cancellationToken = default);
}
