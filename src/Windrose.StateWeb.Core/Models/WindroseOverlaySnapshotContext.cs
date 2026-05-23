using Windrose.StateWeb.Core.Abstractions;

namespace Windrose.StateWeb.Core.Models;

public sealed record WindroseOverlaySnapshotContext : IWindroseOverlaySnapshotSource
{
    public bool LogAvailable { get; init; }
    public string ParserStatus { get; init; } = "Starting";
    public string? ServerName { get; init; }
    public string? CurrentIslandId { get; init; }
    public string? WorldName { get; init; }
    public string? WorldPresetType { get; init; }
    public int ConnectedPlayerCount { get; init; }
    public int TotalPlayerCount { get; init; }
    public int RecentEventCount { get; init; }
    public int RecentHistoryCount { get; init; }
    public int ObservedFamilyCount { get; init; }
    public bool HasStandaloneShipDocument { get; init; }
    public string? LatestBackupAge { get; init; }
    public string? LatestBackupPath { get; init; }
    public IReadOnlyList<string> Highlights { get; init; } = [];
}
