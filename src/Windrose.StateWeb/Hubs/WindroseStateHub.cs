using Microsoft.AspNetCore.SignalR;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.State;

namespace Windrose.StateWeb.Hubs;

public interface IWindroseStateHubClient
{
    Task WindroseStateUpdate(WindroseStateChange change);

    Task WindroseEvent(WindroseEvent evt);
}

public sealed class WindroseStateHub(IWindroseStateStore stateStore) : Hub<IWindroseStateHubClient>
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.WindroseStateUpdate(new WindroseStateChange
        {
            Kind = "ConnectedSnapshot",
            State = stateStore.GetState()
        });

        await base.OnConnectedAsync();
    }
}
