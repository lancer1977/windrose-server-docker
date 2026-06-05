using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Windrose.StateWeb.Options;

namespace Windrose.StateWeb.Services;

public sealed class ChannelCheevosStatePoller(
    HttpClient httpClient,
    IOptions<WindroseStateOptions> options,
    ILogger<ChannelCheevosStatePoller> logger) : IChannelCheevosStatePoller
{
    public async Task<ChannelCheevosPollReadback> PollAsync(CancellationToken cancellationToken = default)
    {
        var stateOptions = options.Value;
        var now = DateTimeOffset.UtcNow;
        var endpoint = stateOptions.ResolveChannelCheevosStateUrl();
        var webKey = stateOptions.ResolveChannelCheevosWebKey();
        var configured = !string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(webKey);

        if (!stateOptions.EnableChannelCheevosPolling)
        {
            return new ChannelCheevosPollReadback
            {
                Enabled = false,
                Configured = configured,
                Target = stateOptions.ResolvedChannelCheevosTarget,
                Endpoint = endpoint ?? string.Empty,
                Status = "disabled",
                Message = "ChannelCheevos polling is disabled.",
                ObservedAtUtc = now
            };
        }

        if (!configured)
        {
            return new ChannelCheevosPollReadback
            {
                Enabled = true,
                Configured = false,
                Target = stateOptions.ResolvedChannelCheevosTarget,
                Endpoint = endpoint ?? string.Empty,
                Status = "unconfigured",
                Message = "ChannelCheevos state endpoint and webkey are required before polling.",
                ObservedAtUtc = now
            };
        }

        try
        {
            var requestUri = AppendWebKey(endpoint!, webKey);
            var snapshot = await httpClient.GetFromJsonAsync<ChannelCheevosStateSnapshot>(requestUri, cancellationToken);
            return new ChannelCheevosPollReadback
            {
                Enabled = true,
                Configured = true,
                Target = stateOptions.ResolvedChannelCheevosTarget,
                Endpoint = endpoint!,
                Status = snapshot is null ? "empty" : "ok",
                Message = snapshot is null ? "ChannelCheevos returned an empty state payload." : "ChannelCheevos state poll succeeded.",
                ObservedAtUtc = DateTimeOffset.UtcNow,
                State = snapshot
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            logger.LogWarning(ex, "ChannelCheevos state poll failed with safe client status {StatusCode} for target {Target}.", ex.StatusCode, stateOptions.ResolvedChannelCheevosTarget);
            return BuildFailure(stateOptions, endpoint!, "rejected", "ChannelCheevos rejected the state poll request.", ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "ChannelCheevos state poll failed for target {Target}.", stateOptions.ResolvedChannelCheevosTarget);
            return BuildFailure(stateOptions, endpoint!, "error", "ChannelCheevos state poll failed.", ex.Message);
        }
    }

    private static ChannelCheevosPollReadback BuildFailure(WindroseStateOptions options, string endpoint, string status, string message, string rawError) => new()
    {
        Enabled = true,
        Configured = true,
        Target = options.ResolvedChannelCheevosTarget,
        Endpoint = endpoint,
        Status = status,
        Message = message,
        ObservedAtUtc = DateTimeOffset.UtcNow,
        RawError = rawError
    };

    private static string AppendWebKey(string endpoint, string webKey)
    {
        var separator = endpoint.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{endpoint}{separator}webkey={Uri.EscapeDataString(webKey)}";
    }
}
