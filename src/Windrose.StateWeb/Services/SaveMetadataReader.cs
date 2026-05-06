using System.IO.Compression;
using System.Text.Json;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.Options;
using Windrose.StateWeb.State;
using Microsoft.Extensions.Options;

namespace Windrose.StateWeb.Services;

public sealed class SaveMetadataReader(
    IOptions<WindroseStateOptions> options,
    IWindroseStateStore stateStore,
    ILogger<SaveMetadataReader> logger) : BackgroundService
{
    private readonly WindroseStateOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, _options.SaveMetadataPollSeconds)));

        do
        {
            stateStore.UpdateSaveMetadata(ReadMetadata());
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private SaveMetadata ReadMetadata()
    {
        try
        {
            var activeIslandId = stateStore.GetState().CurrentIslandId;
            var backupRoot = Path.Combine(_options.SaveRootPath, "RocksDB_v2_Backups", "Worlds");
            var latest = Directory.Exists(backupRoot)
                ? Directory.EnumerateFiles(backupRoot, "*_Latest.zip", SearchOption.AllDirectories)
                    .Select(path => new FileInfo(path))
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .FirstOrDefault()
                : null;

            if (latest is null)
            {
                return new SaveMetadata { ActiveIslandId = activeIslandId, Error = $"No latest backup ZIP found under {backupRoot}" };
            }

            var (worldName, worldPresetType, islandIdFromWorld) = ReadWorldDescription(latest.FullName);
            return new SaveMetadata
            {
                ActiveIslandId = activeIslandId ?? islandIdFromWorld ?? latest.Directory?.Name,
                LatestBackupPath = latest.FullName,
                LatestBackupTime = new DateTimeOffset(latest.LastWriteTimeUtc, TimeSpan.Zero),
                LatestBackupSizeBytes = latest.Length,
                WorldName = worldName,
                WorldPresetType = worldPresetType
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read save metadata");
            return new SaveMetadata { ActiveIslandId = stateStore.GetState().CurrentIslandId, Error = ex.Message };
        }
    }

    private static (string? WorldName, string? WorldPresetType, string? IslandId) ReadWorldDescription(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entry = archive.GetEntry("AdditionalRecordFiles/WorldDescription.json");
        if (entry is null)
        {
            return (null, null, null);
        }

        using var stream = entry.Open();
        using var doc = JsonDocument.Parse(stream);
        if (!doc.RootElement.TryGetProperty("WorldDescription", out var world))
        {
            return (null, null, null);
        }

        return (
            world.TryGetProperty("WorldName", out var name) ? name.GetString() : null,
            world.TryGetProperty("WorldPresetType", out var preset) ? preset.GetString() : null,
            world.TryGetProperty("islandId", out var islandId) ? islandId.GetString() : null);
    }
}
