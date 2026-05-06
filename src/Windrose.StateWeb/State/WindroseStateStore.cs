using System.Collections.Concurrent;
using System.Threading.Channels;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.Options;
using Microsoft.Extensions.Options;

namespace Windrose.StateWeb.State;

public sealed class WindroseStateStore(IOptions<WindroseStateOptions> options) : IWindroseStateStore
{
    private readonly object _gate = new();
    private readonly List<WindroseEvent> _events = [];
    private readonly Dictionary<string, PlayerConnectionState> _players = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, Channel<WindroseEvent>> _subscribers = [];
    private readonly int _eventRetention = Math.Max(100, options.Value.EventRetention);
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
                    .ToArray()
            };
        }
    }

    public void SetLogAvailable(bool available, string? error = null)
    {
        lock (_gate)
        {
            _state = _state with
            {
                LogAvailable = available,
                LastLogRead = available ? DateTimeOffset.UtcNow : _state.LastLogRead,
                ParserStatus = available ? "Running" : "WaitingForLog",
                ParserError = error
            };
        }
    }

    public void Apply(WindroseEvent evt)
    {
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
        }

        foreach (var subscriber in _subscribers.ToArray())
        {
            if (!subscriber.Value.Writer.TryWrite(evt))
            {
                _subscribers.TryRemove(subscriber.Key, out _);
            }
        }
    }

    public void UpdateSaveMetadata(SaveMetadata save)
    {
        lock (_gate)
        {
            _state = _state with
            {
                Save = save,
                CurrentIslandId = _state.CurrentIslandId ?? save.ActiveIslandId
            };
        }
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
