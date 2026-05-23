namespace Windrose.StateWeb.Core.Abstractions;

public interface IWindroseOverlaySnapshotSource
{
    bool LogAvailable { get; }
    string ParserStatus { get; }
    string? ServerName { get; }
    string? CurrentIslandId { get; }
    string? WorldName { get; }
    string? WorldPresetType { get; }
    int ConnectedPlayerCount { get; }
    int TotalPlayerCount { get; }
    int RecentEventCount { get; }
    int RecentHistoryCount { get; }
    int ObservedFamilyCount { get; }
    bool HasStandaloneShipDocument { get; }
    string? LatestBackupAge { get; }
    string? LatestBackupPath { get; }
    IReadOnlyList<string> Highlights { get; }
}
