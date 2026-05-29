using Windrose.StateWeb.Hubs;

namespace Windrose.StateWeb.Api;

public static class WindroseHubEndpoints
{
    public static IEndpointRouteBuilder MapWindroseStateHub(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<WindroseStateHub>("/hubs/windrose-state");
        return endpoints;
    }
}
