namespace Windrose.StateWeb.Domain;

public sealed record SaveMetadata
{
    public string? ActiveIslandId { get; init; }
    public string? WorldIslandId { get; init; }
    public string? LatestBackupPath { get; init; }
    public DateTimeOffset? LatestBackupTime { get; init; }
    public long? LatestBackupSizeBytes { get; init; }
    public string? CheckpointContainerFormat { get; init; }
    public string? CheckpointExtractedPath { get; init; }
    public IReadOnlyList<CheckpointEntrySummary> CheckpointEntries { get; init; } = [];
    public string? WorldPresetType { get; init; }
    public string? WorldName { get; init; }
    public int? WorldSettingCount { get; init; }
    public int? WorldBoolSettingCount { get; init; }
    public int? WorldFloatSettingCount { get; init; }
    public int? WorldTagSettingCount { get; init; }
    public ServerDescriptionMetadata? ServerDescription { get; init; }
    public IReadOnlyList<SaveDocumentSummary> DocumentSummaries { get; init; } = [];
    public IReadOnlyList<SaveCollectionSummary> CollectionSummaries { get; init; } = [];
    public IReadOnlyList<ObservedFamilySummary> ObservedFamilies { get; init; } = [];
    public SaveRecordGraphReport RecordGraph { get; init; } = new();
    public string? Error { get; init; }

    public TimeSpan? BackupAge =>
        LatestBackupTime is null ? null : DateTimeOffset.UtcNow - LatestBackupTime.Value;
}
