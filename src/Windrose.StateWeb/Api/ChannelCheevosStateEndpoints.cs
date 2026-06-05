using Windrose.StateWeb.Services;

namespace Windrose.StateWeb.Api;

public static class ChannelCheevosStateEndpoints
{
    public static IEndpointRouteBuilder MapChannelCheevosStateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/channel-cheevos/state", async (IChannelCheevosStatePoller poller, CancellationToken cancellationToken) =>
        {
            var readback = await poller.PollAsync(cancellationToken);
            return Results.Ok(readback);
        });

        return endpoints;
    }
}
