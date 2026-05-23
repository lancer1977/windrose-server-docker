using Microsoft.AspNetCore.SignalR.Client;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.Options;
using Microsoft.Extensions.Options;

namespace Windrose.StateWeb.Services;

public sealed class SignalRWindroseLivePushPublisher(
    IOptions<WindroseStateOptions> options,
    IWindroseHubConnectionFactory connectionFactory,
    ILogger<SignalRWindroseLivePushPublisher> logger) : IWindroseLivePushPublisher, IAsyncDisposable
{
    private readonly WindroseStateOptions _options = options.Value;
    private readonly IWindroseHubConnectionFactory _connectionFactory = connectionFactory;
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private IWindroseHubConnection? _connection;

    public async Task PublishAsync(WindroseStateChange change, CancellationToken cancellationToken)
    {
        if (!ShouldPush)
        {
            return;
        }

        try
        {
            var connection = await EnsureConnectionAsync(cancellationToken);
            if (connection is null)
            {
                return;
            }

            await connection.SendAsync(_options.ChannelCheevosStateMethod, change, cancellationToken);
            if (change.Event is not null)
            {
                await connection.SendAsync(_options.ChannelCheevosEventMethod, change.Event, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to push Windrose state update to channel-cheevos");
            await ResetConnectionAsync();
        }
    }

    private bool ShouldPush =>
        _options.EnableChannelCheevosPush &&
        !string.IsNullOrWhiteSpace(_options.ResolveChannelCheevosHubUrl()) &&
        !string.IsNullOrWhiteSpace(_options.ResolveChannelCheevosWebKey());

    private async Task<IWindroseHubConnection?> EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { State: HubConnectionState.Connected })
        {
            return _connection;
        }

        await _connectionGate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { State: HubConnectionState.Connected })
            {
                return _connection;
            }

            _connection ??= CreateConnection();

            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync(cancellationToken);
                logger.LogInformation("Connected Windrose live push hub to {HubUrl} for target {Target}", _options.ResolveChannelCheevosHubUrl(), _options.ResolvedChannelCheevosTarget);
            }

            return _connection;
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    private IWindroseHubConnection CreateConnection()
    {
        var hubUrl = _options.ResolveChannelCheevosHubUrl();
        var url = BuildHubUrl(hubUrl!, _options.ResolveChannelCheevosWebKey());
        return _connectionFactory.Create(url);
    }

    private async Task ResetConnectionAsync()
    {
        await _connectionGate.WaitAsync();
        try
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    private static string BuildHubUrl(string hubUrl, string webKey)
    {
        var separatorIndex = hubUrl.IndexOf('?');
        var baseUrl = separatorIndex >= 0 ? hubUrl[..separatorIndex] : hubUrl;
        var existingQuery = separatorIndex >= 0 ? hubUrl[(separatorIndex + 1)..] : string.Empty;
        var encodedWebKey = Uri.EscapeDataString(webKey);

        if (string.IsNullOrWhiteSpace(existingQuery))
        {
            return $"{baseUrl}?webkey={encodedWebKey}";
        }

        return $"{baseUrl}?{existingQuery}&webkey={encodedWebKey}";
    }

    public async ValueTask DisposeAsync()
    {
        await ResetConnectionAsync();
        _connectionGate.Dispose();
    }
}

public interface IWindroseHubConnectionFactory
{
    IWindroseHubConnection Create(string url);
}

public interface IWindroseHubConnection : IAsyncDisposable
{
    HubConnectionState State { get; }
    Task StartAsync(CancellationToken cancellationToken);
    Task SendAsync(string methodName, object? arg, CancellationToken cancellationToken);
}

public sealed class DefaultWindroseHubConnectionFactory : IWindroseHubConnectionFactory
{
    public IWindroseHubConnection Create(string url)
    {
        return new DefaultWindroseHubConnection(new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build());
    }

    private sealed class DefaultWindroseHubConnection(HubConnection inner) : IWindroseHubConnection
    {
        public HubConnectionState State => inner.State;

        public Task StartAsync(CancellationToken cancellationToken) => inner.StartAsync(cancellationToken);

        public Task SendAsync(string methodName, object? arg, CancellationToken cancellationToken) =>
            inner.SendAsync(methodName, arg, cancellationToken);

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
