using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Windrose.StateWeb.Api;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.Hubs;
using Windrose.StateWeb.Services;
using Windrose.StateWeb.State;

namespace Windrose.StateWeb.Tests.Services;

public sealed class WindroseStateHubBroadcastServiceTests
{
    [Fact]
    public async Task BroadcastServiceForwardsStateChangesToHubClients()
    {
        var changes = Channel.CreateUnbounded<WindroseStateChange>();
        var publisher = new RecordingPublisher();

        await using var app = CreateApp(changes.Reader, publisher);
        await app.StartAsync();

        var change = new WindroseStateChange
        {
            Kind = "EventApplied",
            State = new WindroseServerState(),
            Event = new WindroseEvent(DateTimeOffset.Parse("2026-05-21T20:00:00Z"), "PlayerJoined", "Information", "Player joined")
        };

        changes.Writer.TryWrite(change);

        var published = await publisher.WaitForPublishAsync(TimeSpan.FromSeconds(2));

        Assert.Equal("EventApplied", published.Kind);
        Assert.NotNull(published.Event);
        Assert.Equal("PlayerJoined", published.Event!.Type);
    }

    private static WebApplication CreateApp(ChannelReader<WindroseStateChange> changes, RecordingPublisher publisher)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<IWindroseStateStore>(_ => new StubStateStore(changes));
        builder.Services.AddSingleton<IWindroseStateHubPublisher>(publisher);
        builder.Services.AddHostedService<WindroseStateHubBroadcastService>();

        var app = builder.Build();
        app.MapWindroseStateHub();
        return app;
    }

    private sealed class StubStateStore(ChannelReader<WindroseStateChange> changes) : IWindroseStateStore
    {
        public WindroseServerState GetState() => new();

        public void SetLogAvailable(bool available, string? error = null)
        {
            throw new NotImplementedException();
        }

        public void Apply(WindroseEvent evt)
        {
            throw new NotImplementedException();
        }

        public void UpdateSaveMetadata(SaveMetadata save)
        {
            throw new NotImplementedException();
        }

        public ChannelReader<WindroseEvent> Subscribe(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ChannelReader<WindroseStateChange> SubscribeStateChanges(CancellationToken cancellationToken) => changes;
    }

    private sealed class RecordingPublisher : IWindroseStateHubPublisher
    {
        private readonly TaskCompletionSource<WindroseStateChange> _published = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task PublishAsync(WindroseStateChange change, CancellationToken cancellationToken)
        {
            _published.TrySetResult(change);
            return Task.CompletedTask;
        }

        public async Task<WindroseStateChange> WaitForPublishAsync(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(_published.Task, Task.Delay(timeout));
            if (completed != _published.Task)
            {
                throw new TimeoutException("Timed out waiting for hub broadcast.");
            }

            return await _published.Task;
        }
    }
}
