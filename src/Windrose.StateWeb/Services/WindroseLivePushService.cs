using Windrose.StateWeb.Options;
using Windrose.StateWeb.State;
using Microsoft.Extensions.Options;

namespace Windrose.StateWeb.Services;

public sealed class WindroseLivePushService(
    IWindroseStateStore stateStore,
    IWindroseLivePushPublisher publisher,
    IOptions<WindroseStateOptions> options,
    ILogger<WindroseLivePushService> logger) : BackgroundService
{
    private readonly WindroseStateOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableChannelCheevosPush || string.IsNullOrWhiteSpace(_options.ResolveChannelCheevosHubUrl()) || string.IsNullOrWhiteSpace(_options.ResolveChannelCheevosWebKey()))
        {
            logger.LogInformation("Channel-cheevos push is disabled");
            return;
        }

        var reader = stateStore.SubscribeStateChanges(stoppingToken);
        await foreach (var change in reader.ReadAllAsync(stoppingToken))
        {
            await publisher.PublishAsync(change, stoppingToken);
        }
    }
}
