using Windrose.StateWeb.State;

namespace Windrose.StateWeb.Services;

public sealed class WindroseStateHubBroadcastService(
    IWindroseStateStore stateStore,
    IWindroseStateHubPublisher publisher,
    ILogger<WindroseStateHubBroadcastService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = stateStore.SubscribeStateChanges(stoppingToken);
        await foreach (var change in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await publisher.PublishAsync(change, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to broadcast Windrose state change to hub clients");
            }
        }
    }
}
