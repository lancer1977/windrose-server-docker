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
    private string? _lastExtractedCheckpointPath;

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
            var currentState = stateStore.GetState();
            var serverDescription = ReadServerDescription();
            var activeIslandId = currentState.CurrentIslandId ?? serverDescription?.WorldIslandId;

            var backupRoot = Path.Combine(_options.SaveRootPath, "RocksDB_v2_Backups", "Worlds");
            var latest = Directory.Exists(backupRoot)
                ? Directory.EnumerateFiles(backupRoot, "*_Latest.zip", SearchOption.AllDirectories)
                    .Select(path => new FileInfo(path))
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .FirstOrDefault()
                : null;

            if (latest is null)
            {
                return new SaveMetadata
                {
                    ActiveIslandId = activeIslandId,
                    ServerDescription = serverDescription,
                    Error = $"No latest backup ZIP found under {backupRoot}"
                };
            }

            var backupSummary = ReadBackupSummary(latest.FullName);
            var checkpointExtractedPath = ExtractCheckpointArchive(latest.FullName, latest.Directory?.Name, latest.LastWriteTimeUtc, latest.Length);
            var checkpointEntries = checkpointExtractedPath is null
                ? []
                : AnalyzeCheckpointEntries(checkpointExtractedPath);
            return new SaveMetadata
            {
                ActiveIslandId = activeIslandId ?? backupSummary.WorldIslandId ?? latest.Directory?.Name,
                WorldIslandId = backupSummary.WorldIslandId,
                LatestBackupPath = latest.FullName,
                LatestBackupTime = new DateTimeOffset(latest.LastWriteTimeUtc, TimeSpan.Zero),
                LatestBackupSizeBytes = latest.Length,
                CheckpointContainerFormat = "RocksDB block-based SST",
                CheckpointExtractedPath = checkpointExtractedPath,
                CheckpointEntries = checkpointEntries,
                WorldName = backupSummary.WorldName,
                WorldPresetType = backupSummary.WorldPresetType,
                WorldSettingCount = backupSummary.WorldSettingCount,
                WorldBoolSettingCount = backupSummary.WorldBoolSettingCount,
                WorldFloatSettingCount = backupSummary.WorldFloatSettingCount,
                WorldTagSettingCount = backupSummary.WorldTagSettingCount,
                ServerDescription = serverDescription,
                DocumentSummaries = backupSummary.DocumentSummaries,
                CollectionSummaries = backupSummary.CollectionSummaries,
                ObservedFamilies = BuildObservedFamilies(checkpointEntries)
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read save metadata");
            return new SaveMetadata
            {
                ActiveIslandId = stateStore.GetState().CurrentIslandId,
                ServerDescription = ReadServerDescription(),
                CheckpointContainerFormat = "RocksDB block-based SST",
                ObservedFamilies = [],
                Error = ex.Message
            };
        }
    }

    private static IReadOnlyList<ObservedFamilySummary> BuildObservedFamilies(IReadOnlyList<CheckpointEntrySummary> entries)
    {
        return
        [
            CreateFamily(
                "island",
                "present",
                "Island and world geometry families are visible in live checkpoint SSTs.",
                "CommonIsland",
                "LandscapeLocation",
                "Terrains",
                "WorldDescription"),
            CreateFamily(
                "actor",
                "present",
                "Actor family markers are visible in live checkpoint SSTs.",
                "Actor_InteractedPoiIds",
                "Actor_RemovedDialogueActorIds",
                "R5BLActor_BuildingBlock",
                "R5BLActor_MineralNode"),
            CreateFamily(
                "player-in-world-metadata",
                entries.Any(entry => entry.Markers.Contains("R5BLPlayerInWorld", StringComparer.OrdinalIgnoreCase) || entry.ReadableTokens.Contains("R5BLPlayerInWorld", StringComparer.OrdinalIgnoreCase))
                    ? "metadata-only"
                    : "not-observed",
                "Player-in-world family names are visible in RocksDB metadata, but no standalone player document has been decoded yet.",
                "R5BLPlayerInWorld",
                "R5BLPlayer"),
            CreateFamily(
                "ship-reference",
                entries.Any(entry => entry.Markers.Contains("ShipId", StringComparer.OrdinalIgnoreCase) || entry.ReadableTokens.Contains("ShipId", StringComparer.OrdinalIgnoreCase))
                    ? "reference-only"
                    : "not-observed",
                "ShipId appears in live SST payloads, but no standalone R5BLShip document has been found in the current snapshot set.",
                "ShipId"),
            CreateFamily(
                "ship-document",
                "not-observed",
                "No standalone R5BLShip document is present in the current live save tree.",
                "R5BLShip")
        ];
    }

    private static ObservedFamilySummary CreateFamily(string name, string status, string notes, params string[] evidence) =>
        new()
        {
            Name = name,
            Status = status,
            Notes = notes,
            Evidence = evidence
        };

    private static IReadOnlyList<CheckpointEntrySummary> AnalyzeCheckpointEntries(string extractRoot)
    {
        if (!Directory.Exists(extractRoot))
        {
            return [];
        }

        return Directory.EnumerateFiles(extractRoot, "*", SearchOption.AllDirectories)
            .Select(path =>
            {
                var info = new FileInfo(path);
                var relativePath = Path.GetRelativePath(extractRoot, path).Replace('\\', '/');
                return new CheckpointEntrySummary
                {
                    Path = relativePath,
                    SizeBytes = info.Length,
                    Kind = InferKind(relativePath),
                    Markers = CollectCheckpointMarkers(path, info.Length),
                    ReadableTokens = CollectCheckpointTokens(path, info.Length)
                };
            })
            .OrderByDescending(entry => entry.SizeBytes)
            .ThenBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> CollectCheckpointMarkers(string path, long fileSize)
    {
        if (fileSize <= 0)
        {
            return [];
        }

        const int maxBytesToScan = 2 * 1024 * 1024;
        var bufferLength = (int)Math.Min(fileSize, maxBytesToScan);
        if (bufferLength <= 0)
        {
            return [];
        }

        try
        {
            using var stream = File.OpenRead(path);
            var buffer = new byte[bufferLength];
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read <= 0)
            {
                return [];
            }

            var markers = new List<string>();
            var haystack = buffer.AsSpan(0, read);
            foreach (var token in CheckpointMarkerTokens)
            {
                if (ContainsAsciiToken(haystack, token))
                {
                    markers.Add(token);
                }
            }

            return markers;
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<string> CollectCheckpointTokens(string path, long fileSize)
    {
        if (fileSize <= 0)
        {
            return [];
        }

        const int maxBytesToScan = 2 * 1024 * 1024;
        var bufferLength = (int)Math.Min(fileSize, maxBytesToScan);
        if (bufferLength <= 0)
        {
            return [];
        }

        try
        {
            using var stream = File.OpenRead(path);
            var buffer = new byte[bufferLength];
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read <= 0)
            {
                return [];
            }

            var tokens = System.Text.RegularExpressions.Regex
                .Matches(System.Text.Encoding.ASCII.GetString(buffer, 0, read), @"[A-Za-z0-9_./-]{4,}")
                .Select(match => match.Value)
                .Where(token => token.Length <= 80)
                .Where(token => token.Any(char.IsUpper) || token.Contains('_') || token.Contains('/') || token.Contains('.'))
                .Where(token => !token.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(24)
                .ToArray();

            return tokens;
        }
        catch
        {
            return [];
        }
    }

    private static bool ContainsAsciiToken(ReadOnlySpan<byte> haystack, string token)
    {
        var needle = System.Text.Encoding.ASCII.GetBytes(token);
        if (needle.Length == 0 || haystack.Length < needle.Length)
        {
            return false;
        }

        for (var offset = 0; offset <= haystack.Length - needle.Length; offset++)
        {
            if (haystack.Slice(offset, needle.Length).SequenceEqual(needle))
            {
                return true;
            }
        }

        return false;
    }

    private string? ExtractCheckpointArchive(string zipPath, string? islandId, DateTime lastWriteTimeUtc, long sizeBytes)
    {
        try
        {
            var extractRoot = Path.Combine(
                Path.GetTempPath(),
                "windrose-state",
                "checkpoint-extracts",
                string.IsNullOrWhiteSpace(islandId) ? "unknown-island" : islandId,
                $"{Path.GetFileNameWithoutExtension(zipPath)}-{lastWriteTimeUtc.Ticks}-{sizeBytes}");

            if (string.Equals(_lastExtractedCheckpointPath, extractRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(extractRoot))
            {
                return extractRoot;
            }

            Directory.CreateDirectory(extractRoot);
            ZipFile.ExtractToDirectory(zipPath, extractRoot, overwriteFiles: true);
            _lastExtractedCheckpointPath = extractRoot;
            return extractRoot;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to extract checkpoint ZIP {ZipPath}", zipPath);
            return null;
        }
    }

    private ServerDescriptionMetadata? ReadServerDescription()
    {
        if (!File.Exists(_options.ServerDescriptionPath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(_options.ServerDescriptionPath);
            using var doc = JsonDocument.Parse(stream);
            if (!doc.RootElement.TryGetProperty("ServerDescription_Persistent", out var description))
            {
                return null;
            }

            return new ServerDescriptionMetadata
            {
                Source = "ServerDescription.json",
                SourcePath = _options.ServerDescriptionPath,
                LastModified = new DateTimeOffset(File.GetLastWriteTimeUtc(_options.ServerDescriptionPath), TimeSpan.Zero),
                PersistentServerId = ReadString(description, "PersistentServerId"),
                InviteCode = ReadString(description, "InviteCode"),
                IsPasswordProtected = ReadBool(description, "IsPasswordProtected"),
                ServerName = ReadString(description, "ServerName"),
                WorldIslandId = ReadString(description, "WorldIslandId"),
                MaxPlayerCount = ReadInt(description, "MaxPlayerCount"),
                P2pProxyAddress = ReadString(description, "P2pProxyAddress"),
                DirectConnectionProxyAddress = ReadString(description, "DirectConnectionProxyAddress"),
                UseDirectConnection = ReadBool(description, "UseDirectConnection"),
                DirectConnectionServerPort = ReadInt(description, "DirectConnectionServerPort"),
                UserSelectedRegion = ReadString(description, "UserSelectedRegion"),
                DirectConnectionServerAddress = ReadString(description, "DirectConnectionServerAddress")
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read ServerDescription.json");
            return new ServerDescriptionMetadata
            {
                Source = "ServerDescription.json",
                SourcePath = _options.ServerDescriptionPath,
                LastModified = new DateTimeOffset(File.GetLastWriteTimeUtc(_options.ServerDescriptionPath), TimeSpan.Zero)
            };
        }
    }

    private static BackupSummary ReadBackupSummary(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var documentSummaries = new List<SaveDocumentSummary>();
        var collectionSummaries = new Dictionary<string, (int Count, long Bytes)>(StringComparer.OrdinalIgnoreCase);

        string? worldName = null;
        string? worldPresetType = null;
        string? worldIslandId = null;
        int? worldSettingCount = null;
        int? worldBoolSettingCount = null;
        int? worldFloatSettingCount = null;
        int? worldTagSettingCount = null;

        foreach (var entry in archive.Entries.Where(entry => !string.IsNullOrWhiteSpace(entry.Name)))
        {
            var kind = InferKind(entry.FullName);
            collectionSummaries[kind] = collectionSummaries.TryGetValue(kind, out var current)
                ? (current.Count + 1, current.Bytes + entry.Length)
                : (1, entry.Length);

            if (!entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var stream = entry.Open();
            using var doc = JsonDocument.Parse(stream);
            var summary = SummarizeJsonDocument(entry.FullName, kind, entry.Length, doc.RootElement);
            documentSummaries.Add(summary);

            if (entry.FullName.EndsWith("WorldDescription.json", StringComparison.OrdinalIgnoreCase))
            {
                (worldName, worldPresetType, worldIslandId, worldSettingCount, worldBoolSettingCount, worldFloatSettingCount, worldTagSettingCount) =
                    ReadWorldDescription(doc.RootElement);
            }
        }

        return new BackupSummary(
            worldName,
            worldPresetType,
            worldIslandId,
            worldSettingCount,
            worldBoolSettingCount,
            worldFloatSettingCount,
            worldTagSettingCount,
            documentSummaries.OrderByDescending(summary => summary.SizeBytes ?? 0).ToArray(),
            collectionSummaries
                .OrderByDescending(pair => pair.Value.Count)
                .Select(pair => new SaveCollectionSummary
                {
                    Name = pair.Key,
                    Count = pair.Value.Count,
                    TotalBytes = pair.Value.Bytes
                })
                .ToArray());
    }

    private static SaveDocumentSummary SummarizeJsonDocument(string path, string kind, long sizeBytes, JsonElement root)
    {
        var scalars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var counts = new JsonCounts();
        SummarizeElement(root, "$", 0, counts, scalars);

        return new SaveDocumentSummary
        {
            Path = path,
            Kind = kind,
            SizeBytes = sizeBytes,
            ScalarPropertyCount = counts.Scalars,
            ObjectCount = counts.Objects,
            ArrayCount = counts.Arrays,
            ScalarPreview = scalars,
            Notes = BuildNotes(kind, counts)
        };
    }

    private static void SummarizeElement(JsonElement element, string path, int depth, JsonCounts counts, Dictionary<string, string> scalars)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                counts.Objects++;
                foreach (var property in element.EnumerateObject())
                {
                    SummarizeElement(property.Value, $"{path}.{property.Name}", depth + 1, counts, scalars);
                }
                break;
            case JsonValueKind.Array:
                counts.Arrays++;
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    SummarizeElement(item, $"{path}[{index++}]", depth + 1, counts, scalars);
                }
                break;
            default:
                counts.Scalars++;
                if (scalars.Count < 24)
                {
                    scalars[path] = TrimScalar(element);
                }
                break;
        }
    }

    private static string BuildNotes(string kind, JsonCounts counts)
    {
        var hints = new List<string>();
        if (kind.Contains("player", StringComparison.OrdinalIgnoreCase)) hints.Add("player");
        if (kind.Contains("ship", StringComparison.OrdinalIgnoreCase)) hints.Add("ship");
        if (kind.Contains("actor", StringComparison.OrdinalIgnoreCase)) hints.Add("actor");
        if (kind.Contains("world", StringComparison.OrdinalIgnoreCase)) hints.Add("world");
        if (kind.Contains("quest", StringComparison.OrdinalIgnoreCase)) hints.Add("quest");
        if (kind.Contains("inventory", StringComparison.OrdinalIgnoreCase)) hints.Add("inventory");
        hints.Add($"{counts.Objects} objects");
        hints.Add($"{counts.Arrays} arrays");
        hints.Add($"{counts.Scalars} scalars");
        return string.Join(", ", hints);
    }

    private static (string? WorldName, string? WorldPresetType, string? WorldIslandId, int? SettingCount, int? BoolCount, int? FloatCount, int? TagCount) ReadWorldDescription(JsonElement root)
    {
        if (!TryGetPropertyIgnoreCase(root, "WorldDescription", out var world))
        {
            return (null, null, null, null, null, null, null);
        }

        var settings = world.TryGetProperty("WorldSettings", out var worldSettings) && worldSettings.ValueKind == JsonValueKind.Object
            ? CountWorldSettings(worldSettings)
            : (0, 0, 0, 0);

        return (
            ReadStringIgnoreCase(world, "WorldName"),
            ReadStringIgnoreCase(world, "WorldPresetType"),
            ReadStringIgnoreCase(world, "IslandId") ?? ReadStringIgnoreCase(world, "islandId"),
            settings.Item1,
            settings.Item2,
            settings.Item3,
            settings.Item4);
    }

    private static (int Total, int BoolCount, int FloatCount, int TagCount) CountWorldSettings(JsonElement worldSettings)
    {
        var boolCount = CountPropertyEntries(worldSettings, "BoolParameters");
        var floatCount = CountPropertyEntries(worldSettings, "FloatParameters");
        var tagCount = CountPropertyEntries(worldSettings, "TagParameters");
        return (boolCount + floatCount + tagCount, boolCount, floatCount, tagCount);
    }

    private static int CountPropertyEntries(JsonElement parent, string propertyName)
    {
        return parent.TryGetProperty(propertyName, out var child) && child.ValueKind == JsonValueKind.Object
            ? child.EnumerateObject().Count()
            : 0;
    }

    private static string InferKind(string path)
    {
        var trimmed = path.Replace('\\', '/');
        if (trimmed.Contains("WorldDescription.json", StringComparison.OrdinalIgnoreCase))
        {
            return "world-description";
        }

        if (trimmed.Contains("player", StringComparison.OrdinalIgnoreCase))
        {
            return "player";
        }

        if (trimmed.Contains("ship", StringComparison.OrdinalIgnoreCase))
        {
            return "ship";
        }

        if (trimmed.Contains("actor", StringComparison.OrdinalIgnoreCase))
        {
            return "actor";
        }

        if (trimmed.Contains("inventory", StringComparison.OrdinalIgnoreCase))
        {
            return "inventory";
        }

        if (trimmed.Contains("quest", StringComparison.OrdinalIgnoreCase))
        {
            return "quest";
        }

        if (trimmed.Contains("world", StringComparison.OrdinalIgnoreCase))
        {
            return "world";
        }

        return Path.GetExtension(trimmed).Trim('.').ToLowerInvariant() switch
        {
            "json" => "json",
            "sst" => "sst",
            "log" => "log",
            "txt" => "txt",
            _ => "other"
        };
    }

    private static string TrimScalar(JsonElement element)
    {
        var value = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Undefined => "undefined",
            _ => element.ToString()
        };

        return value.Length <= 180 ? value : value[..180];
    }

    private static readonly string[] CheckpointMarkerTokens =
    [
        "R5BLPlayerInWorld",
        "R5BLPlayer",
        "R5BLShip",
        "R5BLActor_BuildingBlock",
        "R5BLActor_MineralNode",
        "R5BLIslandChest",
        "Location",
        "Rotation",
        "Inventory",
        "Quest"
    ];

    private static string? ReadString(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out var child) ? child.GetString() : null;

    private static string? ReadStringIgnoreCase(JsonElement parent, string propertyName) =>
        TryGetPropertyIgnoreCase(parent, propertyName, out var child) ? child.GetString() : null;

    private static int? ReadInt(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out var child) && child.TryGetInt32(out var value) ? value : null;

    private static bool? ReadBool(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out var child) && child.ValueKind is JsonValueKind.True or JsonValueKind.False ? child.GetBoolean() : null;

    private static bool TryGetPropertyIgnoreCase(JsonElement parent, string propertyName, out JsonElement child)
    {
        if (parent.TryGetProperty(propertyName, out child))
        {
            return true;
        }

        foreach (var property in parent.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                child = property.Value;
                return true;
            }
        }

        child = default;
        return false;
    }

    private sealed record BackupSummary(
        string? WorldName,
        string? WorldPresetType,
        string? WorldIslandId,
        int? WorldSettingCount,
        int? WorldBoolSettingCount,
        int? WorldFloatSettingCount,
        int? WorldTagSettingCount,
        IReadOnlyList<SaveDocumentSummary> DocumentSummaries,
        IReadOnlyList<SaveCollectionSummary> CollectionSummaries);

    private sealed class JsonCounts
    {
        public int Scalars { get; set; }
        public int Objects { get; set; }
        public int Arrays { get; set; }
    }
}
