using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Windrose.StateWeb.Options;
using Windrose.StateWeb.Services;
using Windrose.StateWeb.Domain;
using Microsoft.AspNetCore.SignalR.Client;

namespace Windrose.StateWeb.Tests.Services;

public sealed class SignalRWindroseLivePushPublisherTests
{
    [Fact]
    public void AppendsWebKeyQueryStringWhenTheHubUrlHasNoQuery()
    {
        var url = InvokeBuildHubUrl("https://example.test/gameplay", "abc-123");

        Assert.Equal("https://example.test/gameplay?webkey=abc-123", url);
    }

    [Fact]
    public void PreservesExistingQueryStringWhenAppendingTheWebKey()
    {
        var url = InvokeBuildHubUrl("https://example.test/gameplay?foo=bar", "abc-123");

        Assert.Equal("https://example.test/gameplay?foo=bar&webkey=abc-123", url);
    }

    [Fact]
    public void EncodesTheWebKeyValue()
    {
        var url = InvokeBuildHubUrl("https://example.test/gameplay", "abc 123");

        Assert.Equal("https://example.test/gameplay?webkey=abc%20123", url);
    }

    [Fact]
    public async Task SwallowsConnectionFailuresAtStartup()
    {
        var factory = new QueueWindroseHubConnectionFactory(
            new ThrowingWindroseHubConnection(),
            new RecordingWindroseHubConnection());
        var options = Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions
        {
            EnableChannelCheevosPush = true,
            ChannelCheevosHubUrl = "https://example.test/windrose-state",
            ChannelCheevosWebKey = "abc-123"
        });
        await using var publisher = new SignalRWindroseLivePushPublisher(options, factory, NullLogger<SignalRWindroseLivePushPublisher>.Instance);

        await publisher.PublishAsync(new WindroseStateChange
        {
            Kind = "startup",
            State = new()
        }, CancellationToken.None);

        await publisher.PublishAsync(new WindroseStateChange
        {
            Kind = "retry",
            State = new()
        }, CancellationToken.None);

        Assert.True(factory.CreatedConnections.OfType<RecordingWindroseHubConnection>().Single().SentMethods.Count >= 1);
    }

    [Fact]
    public async Task RetriesWithAFreshConnectionAfterASendFailure()
    {
        var first = new FailingSendWindroseHubConnection();
        var second = new RecordingWindroseHubConnection();
        var factory = new QueueWindroseHubConnectionFactory(first, second);
        var options = Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions
        {
            EnableChannelCheevosPush = true,
            ChannelCheevosHubUrl = "https://example.test/windrose-state",
            ChannelCheevosWebKey = "abc-123"
        });
        await using var publisher = new SignalRWindroseLivePushPublisher(options, factory, NullLogger<SignalRWindroseLivePushPublisher>.Instance);

        await publisher.PublishAsync(new WindroseStateChange
        {
            Kind = "initial",
            State = new(),
            Event = new WindroseEvent(
                DateTimeOffset.UtcNow,
                "player-joined",
                "info",
                "player joined")
        }, CancellationToken.None);

        await publisher.PublishAsync(new WindroseStateChange
        {
            Kind = "after-reset",
            State = new(),
            Event = new WindroseEvent(
                DateTimeOffset.UtcNow,
                "player-disconnected",
                "info",
                "player disconnected")
        }, CancellationToken.None);

        Assert.Equal(1, first.SendCalls);
        Assert.Equal(2, factory.CreatedConnections.Count);
        Assert.Contains(second.SentMethods, item => item.method == "WindroseStateUpdate");
        Assert.Contains(second.SentMethods, item => item.method == "WindroseEvent");
    }

    private static string InvokeBuildHubUrl(string hubUrl, string webKey)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions());
        var publisher = new SignalRWindroseLivePushPublisher(
            options,
            new SingleWindroseHubConnectionFactory(new RecordingWindroseHubConnection()),
            NullLogger<SignalRWindroseLivePushPublisher>.Instance);
        var method = typeof(SignalRWindroseLivePushPublisher).GetMethod("BuildHubUrl", BindingFlags.NonPublic | BindingFlags.Static);
        return (string)method!.Invoke(null, [hubUrl, webKey])!;
    }

    private sealed class SingleWindroseHubConnectionFactory(IWindroseHubConnection connection) : IWindroseHubConnectionFactory
    {
        public IWindroseHubConnection Create(string url) => connection;
    }

    private sealed class QueueWindroseHubConnectionFactory(params IWindroseHubConnection[] connections) : IWindroseHubConnectionFactory
    {
        private readonly Queue<IWindroseHubConnection> _connections = new(connections);
        public List<IWindroseHubConnection> CreatedConnections { get; } = [];

        public IWindroseHubConnection Create(string url)
        {
            var next = _connections.Dequeue();
            CreatedConnections.Add(next);
            return next;
        }
    }

    private sealed class RecordingWindroseHubConnection : IWindroseHubConnection
    {
        public HubConnectionState State { get; private set; } = HubConnectionState.Disconnected;
        public List<(string method, object? arg)> SentMethods { get; } = [];

        public Task StartAsync(CancellationToken cancellationToken)
        {
            State = HubConnectionState.Connected;
            return Task.CompletedTask;
        }

        public Task SendAsync(string methodName, object? arg, CancellationToken cancellationToken)
        {
            SentMethods.Add((methodName, arg));
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            State = HubConnectionState.Disconnected;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingWindroseHubConnection : IWindroseHubConnection
    {
        public HubConnectionState State { get; } = HubConnectionState.Disconnected;
        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new HttpRequestException("startup failed");
        }

        public Task SendAsync(string methodName, object? arg, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("should not send");
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FailingSendWindroseHubConnection : IWindroseHubConnection
    {
        public HubConnectionState State { get; private set; } = HubConnectionState.Disconnected;
        public int SendCalls { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            State = HubConnectionState.Connected;
            return Task.CompletedTask;
        }

        public Task SendAsync(string methodName, object? arg, CancellationToken cancellationToken)
        {
            SendCalls++;
            throw new HttpRequestException("send failed");
        }

        public ValueTask DisposeAsync()
        {
            State = HubConnectionState.Disconnected;
            return ValueTask.CompletedTask;
        }
    }
}
