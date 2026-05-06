using System.Text.Json;
using Windrose.StateWeb.State;

namespace Windrose.StateWeb.Api;

public static class WindroseEndpoints
{
    public static IEndpointRouteBuilder MapWindroseStateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", (IWindroseStateStore store) =>
        {
            var state = store.GetState();
            return Results.Ok(new
            {
                status = state.LogAvailable ? "ok" : "degraded",
                state.LogAvailable,
                state.LastLogRead,
                state.ParserStatus,
                state.ParserError,
                saveError = state.Save.Error
            });
        });

        endpoints.MapGet("/api/state", (IWindroseStateStore store) => store.GetState());
        endpoints.MapGet("/api/players", (IWindroseStateStore store) => store.GetState().Players);
        endpoints.MapGet("/api/events", (IWindroseStateStore store) => store.GetState().RecentEvents);
        endpoints.MapGet("/api/saves/latest", (IWindroseStateStore store) => store.GetState().Save);
        endpoints.MapGet("/api/world/description", (IWindroseStateStore store) =>
        {
            var save = store.GetState().Save;
            return Results.Ok(new
            {
                save.ActiveIslandId,
                save.WorldName,
                save.WorldPresetType,
                save.LatestBackupPath,
                save.LatestBackupTime
            });
        });

        endpoints.MapGet("/api/events/stream", async (HttpContext context, IWindroseStateStore store) =>
        {
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Content-Type", "text/event-stream");

            var reader = store.Subscribe(context.RequestAborted);
            await foreach (var evt in reader.ReadAllAsync(context.RequestAborted))
            {
                await context.Response.WriteAsync($"event: {evt.Type}\n", context.RequestAborted);
                await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(evt)}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        });

        return endpoints;
    }
}
