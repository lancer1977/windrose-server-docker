using Microsoft.AspNetCore.SignalR;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.Hubs;

namespace Windrose.StateWeb.Services;

public sealed class SignalRWindroseStateHubPublisher(
    IHubContext<WindroseStateHub, IWindroseStateHubClient> hubContext) : IWindroseStateHubPublisher
{
    public async Task PublishAsync(WindroseStateChange change, CancellationToken cancellationToken)
    {
        await hubContext.Clients.All.WindroseStateUpdate(change);

        if (change.Event is not null)
        {
            await hubContext.Clients.All.WindroseEvent(change.Event);
        }
    }
}
