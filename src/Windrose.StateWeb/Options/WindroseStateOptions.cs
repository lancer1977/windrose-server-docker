namespace Windrose.StateWeb.Options;

public sealed class WindroseStateOptions
{
    public int Port { get; set; } = 8781;
    public string ServerFilesPath { get; set; } = "/server-files";
    public string LogRelativePath { get; set; } = "R5/Saved/Logs/R5.log";
    public string SaveRootRelativePath { get; set; } = "R5/Saved/SaveProfiles/Default";
    public string ServerDescriptionRelativePath { get; set; } = "R5/ServerDescription.json";
    public string SnapshotPath { get; set; } = "/tmp/windrose-state/current-state.json";
    public int EventRetention { get; set; } = 500;
    public int SaveMetadataPollSeconds { get; set; } = 30;
    public bool TailFromEnd { get; set; }
    public bool EnableChannelCheevosPush { get; set; }
    public bool EnableChannelCheevosPolling { get; set; }
    public string ChannelCheevosTarget { get; set; } = "prod";
    public string? ChannelCheevosBaseUrl { get; set; }
    public string? ChannelCheevosBaseUrlDev { get; set; }
    public string? ChannelCheevosBaseUrlDebug { get; set; }
    public string? ChannelCheevosBaseUrlProd { get; set; }
    public string? ChannelCheevosStateUrl { get; set; }
    public string? ChannelCheevosStateUrlDev { get; set; }
    public string? ChannelCheevosStateUrlDebug { get; set; }
    public string? ChannelCheevosStateUrlProd { get; set; }
    public string? ChannelCheevosHubUrl { get; set; }
    public string? ChannelCheevosHubUrlDev { get; set; }
    public string? ChannelCheevosHubUrlDebug { get; set; }
    public string? ChannelCheevosHubUrlProd { get; set; }
    public string ChannelCheevosWebKey { get; set; } = "";
    public string? ChannelCheevosWebKeyDev { get; set; }
    public string? ChannelCheevosWebKeyDebug { get; set; }
    public string? ChannelCheevosWebKeyProd { get; set; }
    public string ChannelCheevosStateMethod { get; set; } = "WindroseStateUpdate";
    public string ChannelCheevosEventMethod { get; set; } = "WindroseEvent";
    public bool RedactSensitiveMetadata { get; set; }
    public string PluginBridgeRelativePath { get; set; } = "windrose_plugin_bridge";
    public bool PluginBridgeDevExecutionEnabled { get; set; }

    public string LogPath => Path.Combine(ServerFilesPath, LogRelativePath);
    public string SaveRootPath => Path.Combine(ServerFilesPath, SaveRootRelativePath);
    public string ServerDescriptionPath => Path.Combine(ServerFilesPath, ServerDescriptionRelativePath);
    public string PluginBridgePath => Path.Combine(ServerFilesPath, PluginBridgeRelativePath);
    public string PluginBridgeStatusPath => Path.Combine(PluginBridgePath, "status.json");
    public string PluginBridgeActionsPath => Path.Combine(PluginBridgePath, "actions");
    public string PluginBridgeResultsPath => Path.Combine(PluginBridgePath, "results");
    public string PluginBridgeConfigPath => Path.Combine(PluginBridgePath, "config.json");

    public string? ResolveChannelCheevosHubUrl()
    {
        var target = NormalizeTarget(ChannelCheevosTarget);
        return target switch
        {
            "dev" => ChannelCheevosHubUrlDev ?? ChannelCheevosHubUrl,
            "debug" => ChannelCheevosHubUrlDebug ?? ChannelCheevosHubUrl,
            "prod" => ChannelCheevosHubUrlProd ?? ChannelCheevosHubUrl,
            _ => ChannelCheevosHubUrl
        };
    }

    public string? ResolveChannelCheevosBaseUrl()
    {
        var target = NormalizeTarget(ChannelCheevosTarget);
        var configured = target switch
        {
            "dev" => ChannelCheevosBaseUrlDev ?? ChannelCheevosBaseUrl,
            "debug" => ChannelCheevosBaseUrlDebug ?? ChannelCheevosBaseUrl,
            "prod" => ChannelCheevosBaseUrlProd ?? ChannelCheevosBaseUrl,
            _ => ChannelCheevosBaseUrl
        };

        return string.IsNullOrWhiteSpace(configured) ? TryResolveOrigin(ResolveChannelCheevosHubUrl()) : configured.TrimEnd('/');
    }

    public string? ResolveChannelCheevosStateUrl()
    {
        var target = NormalizeTarget(ChannelCheevosTarget);
        var configured = target switch
        {
            "dev" => ChannelCheevosStateUrlDev ?? ChannelCheevosStateUrl,
            "debug" => ChannelCheevosStateUrlDebug ?? ChannelCheevosStateUrl,
            "prod" => ChannelCheevosStateUrlProd ?? ChannelCheevosStateUrl,
            _ => ChannelCheevosStateUrl
        };

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var baseUrl = ResolveChannelCheevosBaseUrl();
        return string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl}/api/windrose/state";
    }

    public string ResolveChannelCheevosWebKey()
    {
        var target = NormalizeTarget(ChannelCheevosTarget);
        return target switch
        {
            "dev" => ChannelCheevosWebKeyDev ?? ChannelCheevosWebKey,
            "debug" => ChannelCheevosWebKeyDebug ?? ChannelCheevosWebKey,
            "prod" => ChannelCheevosWebKeyProd ?? ChannelCheevosWebKey,
            _ => ChannelCheevosWebKey
        };
    }

    public string ResolvedChannelCheevosTarget => NormalizeTarget(ChannelCheevosTarget);

    private static string NormalizeTarget(string? target)
    {
        return string.IsNullOrWhiteSpace(target)
            ? "prod"
            : target.Trim().ToLowerInvariant();
    }

    private static string? TryResolveOrigin(string? absoluteUrl)
    {
        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return uri.GetLeftPart(UriPartial.Authority);
    }
}
