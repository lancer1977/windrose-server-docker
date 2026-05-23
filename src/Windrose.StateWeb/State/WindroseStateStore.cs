using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Text.Json;
using Windrose.StateWeb.Core.Contracts;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.Options;
using Microsoft.Extensions.Options;

namespace Windrose.StateWeb.State;

public sealed class WindroseStateStore(
    IOptions<WindroseStateOptions> options,
    ILogger<WindroseStateStore> logger) : IWindroseStateStore
{
    private readonly object _gate = new();
    private readonly WindroseStateOptions _options = options.Value;
    private readonly List<WindroseEvent> _events = [];
    private readonly List<WindroseTimelineEntry> _history = [];
    private readonly Dictionary<string, PlayerConnectionState> _players = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, Channel<WindroseEvent>> _subscribers = [];
    private readonly ConcurrentDictionary<Guid, Channel<WindroseStateChange>> _stateSubscribers = [];
    private readonly int _eventRetention = Math.Max(100, options.Value.EventRetention);
    private readonly int _historyRetention = Math.Max(100, options.Value.EventRetention);
    private WindroseServerState _state = new();

    public WindroseServerState GetState()
    {
        lock (_gate)
        {
            return _state with
            {
                Players = _players.Values.OrderByDescending(p => p.LastSeen).ToArray(),
                RecentEvents = _events.OrderByDescending(e => e.Timestamp).Take(_eventRetention).ToArray(),
                RecentWarnings = _events
                    .Where(e => e.Severity is "Warning" or "Error")
                    .OrderByDescending(e => e.Timestamp)
                    .Take(20)
                    .ToArray(),
                RecentHistory = _history.OrderByDescending(entry => entry.Timestamp).Take(_historyRetention).ToArray()
            };
        }
    }

    public void SetLogAvailable(bool available, string? error = null)
    {
        WindroseStateChange change;
        lock (_gate)
        {
            _state = _state with
            {
                LogAvailable = available,
                LastLogRead = available ? DateTimeOffset.UtcNow : _state.LastLogRead,
                ParserStatus = available ? "Running" : "WaitingForLog",
                ParserError = error
            };
            RecordHistory(new WindroseTimelineEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Category = "State",
                Type = "LogAvailabilityChanged",
                Severity = available ? "Information" : "Warning",
                Message = available ? "Log file is available" : "Log file is missing",
                Source = "state-store",
                Properties = new Dictionary<string, string>
                {
                    ["available"] = available.ToString(),
                    ["error"] = error ?? ""
                }
            });
            change = new WindroseStateChange
            {
                Kind = "LogAvailabilityChanged",
                Timestamp = DateTimeOffset.UtcNow,
                State = SnapshotStateUnsafe(),
                Notes = error
            };
        }

        PublishStateChange(change);
        PersistSnapshot(change.State);
    }

    public void Apply(WindroseEvent evt)
    {
        WindroseStateChange change;
        lock (_gate)
        {
            _events.Add(evt);
            if (_events.Count > _eventRetention)
            {
                _events.RemoveRange(0, _events.Count - _eventRetention);
            }

            _state = ReduceServerState(_state, evt) with
            {
                LastEventAt = evt.Timestamp,
                LastLogRead = DateTimeOffset.UtcNow,
                ParserStatus = "Running",
                ParserError = null
            };

            ReducePlayerState(evt);
            RecordHistory(new WindroseTimelineEntry
            {
                Timestamp = evt.Timestamp,
                Category = "Event",
                Type = evt.Type,
                Severity = evt.Severity,
                Message = evt.Message,
                SessionId = evt.SessionId,
                AccountId = evt.AccountId,
                ClientName = evt.ClientName,
                IslandId = _state.CurrentIslandId,
                Source = "log",
                Properties = evt.Properties ?? new Dictionary<string, string>()
            });
            change = new WindroseStateChange
            {
                Kind = "EventApplied",
                Timestamp = evt.Timestamp,
                State = SnapshotStateUnsafe(),
                Event = evt
            };
        }

        foreach (var subscriber in _subscribers.ToArray())
        {
            if (!subscriber.Value.Writer.TryWrite(evt))
            {
                _subscribers.TryRemove(subscriber.Key, out _);
            }
        }

        PublishStateChange(change);
        PersistSnapshot(change.State);
    }

    public void UpdateSaveMetadata(SaveMetadata save)
    {
        WindroseStateChange change;
        lock (_gate)
        {
            _state = _state with
            {
                Save = save,
                CurrentIslandId = _state.CurrentIslandId ?? save.ActiveIslandId ?? save.WorldIslandId,
                ServerDescription = save.ServerDescription ?? _state.ServerDescription,
                ServerName = save.ServerDescription?.ServerName ?? _state.ServerName ?? save.WorldName,
                InviteCode = save.ServerDescription?.InviteCode ?? _state.InviteCode,
                MaxPlayers = save.ServerDescription?.MaxPlayerCount ?? _state.MaxPlayers,
                UseDirectConnection = save.ServerDescription?.UseDirectConnection ?? _state.UseDirectConnection,
                DirectConnectionServerPort = save.ServerDescription?.DirectConnectionServerPort ?? _state.DirectConnectionServerPort
            };
            RecordHistory(new WindroseTimelineEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Category = "State",
                Type = "SaveMetadataUpdated",
                Severity = "Information",
                Message = save.Error is null ? "Latest save metadata refreshed" : $"Latest save metadata refreshed with warning: {save.Error}",
                IslandId = _state.CurrentIslandId ?? save.ActiveIslandId ?? save.WorldIslandId,
                Source = "save-metadata",
                Properties = new Dictionary<string, string>
                {
                    ["worldName"] = save.WorldName ?? "",
                    ["worldPresetType"] = save.WorldPresetType ?? "",
                    ["checkpointContainerFormat"] = save.CheckpointContainerFormat ?? ""
                }
            });
            change = new WindroseStateChange
            {
                Kind = "SaveMetadataUpdated",
                Timestamp = DateTimeOffset.UtcNow,
                State = SnapshotStateUnsafe()
            };
        }

        PublishStateChange(change);
        PersistSnapshot(change.State);
    }

    public ChannelReader<WindroseEvent> Subscribe(CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<WindroseEvent>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        _subscribers[id] = channel;
        cancellationToken.Register(() =>
        {
            if (_subscribers.TryRemove(id, out var removed))
            {
                removed.Writer.TryComplete();
            }
        });

        return channel.Reader;
    }

    public ChannelReader<WindroseStateChange> SubscribeStateChanges(CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<WindroseStateChange>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        _stateSubscribers[id] = channel;
        cancellationToken.Register(() =>
        {
            if (_stateSubscribers.TryRemove(id, out var removed))
            {
                removed.Writer.TryComplete();
            }
        });

        return channel.Reader;
    }

    private static WindroseServerState ReduceServerState(WindroseServerState state, WindroseEvent evt)
    {
        if (evt.Type == "ServerInitialized" && evt.Properties?.TryGetValue("islandId", out var initializedIsland) == true)
        {
            return state with { CurrentIslandId = initializedIsland };
        }

        if (evt.Type == "ServerReady")
        {
            return state with { IsReady = true };
        }

        if (evt.Type != "ServerSettingsObserved" || evt.Properties is null)
        {
            return state;
        }

        var next = state;
        foreach (var property in evt.Properties)
        {
            next = property.Key switch
            {
                "WorldIslandId" => next with { CurrentIslandId = property.Value },
                "ServerName" => next with { ServerName = property.Value },
                "InviteCode" => next with { InviteCode = property.Value },
                "MaxPlayerCount" when int.TryParse(property.Value, out var maxPlayers) => next with { MaxPlayers = maxPlayers },
                "UseDirectConnection" when bool.TryParse(property.Value, out var direct) => next with { UseDirectConnection = direct },
                "DirectConnectionServerPort" when int.TryParse(property.Value, out var port) => next with { DirectConnectionServerPort = port },
                _ => next
            };
        }

        return next;
    }

    private void ReducePlayerState(WindroseEvent evt)
    {
        if (!evt.Type.StartsWith("Player", StringComparison.Ordinal))
        {
            return;
        }

        var key = evt.SessionId ?? evt.AccountId ?? evt.ClientName;
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _players.TryGetValue(key, out var current);
        current ??= new PlayerConnectionState
        {
            Key = key,
            FirstSeen = evt.Timestamp,
            LastSeen = evt.Timestamp
        };

        var isDisconnect = evt.Type == "PlayerDisconnected";
        var next = current with
        {
            SessionId = evt.SessionId ?? current.SessionId,
            AccountId = evt.AccountId ?? current.AccountId,
            ClientName = string.IsNullOrWhiteSpace(evt.ClientName) ? current.ClientName : evt.ClientName,
            Phase = PhaseFor(evt.Type),
            IsConnected = !isDisconnect,
            LastSeen = evt.Timestamp,
            DisconnectedAt = isDisconnect ? evt.Timestamp : null,
            DisconnectReason = isDisconnect && evt.Properties?.TryGetValue("disconnectReason", out var reason) == true ? reason : current.DisconnectReason
        };

        _players[key] = next;
    }

    private WindroseServerState SnapshotStateUnsafe() => _state with
    {
        Players = _players.Values.OrderByDescending(p => p.LastSeen).ToArray(),
        RecentEvents = _events.OrderByDescending(e => e.Timestamp).Take(_eventRetention).ToArray(),
        RecentWarnings = _events
            .Where(e => e.Severity is "Warning" or "Error")
            .OrderByDescending(e => e.Timestamp)
            .Take(20)
            .ToArray()
    };

    private void PublishStateChange(WindroseStateChange change)
    {
        foreach (var subscriber in _stateSubscribers.ToArray())
        {
            if (!subscriber.Value.Writer.TryWrite(change))
            {
                _stateSubscribers.TryRemove(subscriber.Key, out _);
            }
        }
    }

    private void RecordHistory(WindroseTimelineEntry entry)
    {
        _history.Add(entry);
        if (_history.Count > _historyRetention)
        {
            _history.RemoveRange(0, _history.Count - _historyRetention);
        }
    }

    private void PersistSnapshot(WindroseServerState? state)
    {
        if (state is null || string.IsNullOrWhiteSpace(_options.SnapshotPath))
        {
            return;
        }

        try
        {
            var snapshotPath = _options.SnapshotPath;
            var directory = Path.GetDirectoryName(snapshotPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(state);
            File.WriteAllText(snapshotPath, json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist Windrose state snapshot to {SnapshotPath}", _options.SnapshotPath);
        }
    }

    private static string PhaseFor(string type) => type switch
    {
        "PlayerReserved" => "Reserved",
        "PlayerBlConnected" => "BL connected",
        "PlayerUeConnected" => "UE connected",
        "PlayerLoginRequested" => "Login",
        "PlayerJoined" => "Joined",
        "PlayerDisconnected" => "Disconnected",
        _ => "Observed"
    };
}
