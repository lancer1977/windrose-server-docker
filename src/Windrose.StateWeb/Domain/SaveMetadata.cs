namespace Windrose.StateWeb.Domain;

public sealed record SaveMetadata
{
    public string? ActiveIslandId { get; init; }
    public string? LatestBackupPath { get; init; }
    public DateTimeOffset? LatestBackupTime { get; init; }
    public long? LatestBackupSizeBytes { get; init; }
    public string? WorldPresetType { get; init; }
    public string? WorldName { get; init; }
    public string? Error { get; init; }

    public TimeSpan? BackupAge =>
        LatestBackupTime is null ? null : DateTimeOffset.UtcNow - LatestBackupTime.Value;
}
