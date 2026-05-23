using System.IO.Compression;
using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.Options;
using Windrose.StateWeb.Services;
using Windrose.StateWeb.State;

namespace Windrose.StateWeb.Tests.Services;

public sealed class SaveMetadataReaderTests
{
    [Fact]
    public void ReadsServerDescriptionAndBackupSummaryFromTempFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "windrose-state-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var serverFiles = Path.Combine(root, "server-files");
        var saveRoot = Path.Combine(serverFiles, "R5", "Saved", "SaveProfiles", "Default", "RocksDB_v2_Backups", "Worlds", "F3B27E1F83434AF5A1BBA9B40E848A42");
        Directory.CreateDirectory(saveRoot);

        File.WriteAllText(
            Path.Combine(serverFiles, "R5", "ServerDescription.json"),
            """
            {
              "ServerDescription_Persistent": {
                "PersistentServerId": "persist-123",
                "InviteCode": "invite-123",
                "IsPasswordProtected": false,
                "ServerName": "Windrose Test",
                "WorldIslandId": "F3B27E1F83434AF5A1BBA9B40E848A42",
                "MaxPlayerCount": 10,
                "P2pProxyAddress": "127.0.0.1",
                "DirectConnectionProxyAddress": "0.0.0.0",
                "UseDirectConnection": false,
                "DirectConnectionServerPort": 7777,
                "UserSelectedRegion": "EU",
                "DirectConnectionServerAddress": ""
              }
            }
            """);

        var zipPath = Path.Combine(saveRoot, "World_001_Latest.zip");
        using (var file = File.Create(zipPath))
        using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
        {
            AddEntry(
                archive,
                "RocksDB/000123.sst",
                """
                R5BLPlayerInWorld R5BLPlayer R5BLShip R5BLActor_BuildingBlock Location Rotation Inventory Quest
                Blocks DataKey MarkupKey IslandId ShipId ChangeRevision _guid _meta _version
                """);

            AddEntry(
                archive,
                "AdditionalRecordFiles/WorldDescription.json",
                """
                {
                  "WorldDescription": {
                    "IslandId": "F3B27E1F83434AF5A1BBA9B40E848A42",
                    "WorldName": "Test World",
                    "WorldPresetType": "Custom",
                    "WorldSettings": {
                      "BoolParameters": {
                        "a": true,
                        "b": false
                      },
                      "FloatParameters": {
                        "c": 1.5
                      },
                      "TagParameters": {
                        "d": {
                          "TagName": "WDS.Parameter.CombatDifficulty.Normal"
                        }
                      }
                    }
                  }
                }
                """);

            AddEntry(
                archive,
                "AdditionalRecordFiles/PlayerSnapshot.json",
                """
                {
                  "Player": {
                    "Name": "Test Player",
                    "Location": {
                      "X": 1,
                      "Y": 2,
                      "Z": 3
                    }
                  }
                }
                """);
        }

        var options = Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions
        {
            ServerFilesPath = serverFiles,
            SaveRootRelativePath = "R5/Saved/SaveProfiles/Default"
        });
        var reader = new SaveMetadataReader(options, new StubStateStore(), NullLogger<SaveMetadataReader>.Instance);

        var method = typeof(SaveMetadataReader).GetMethod("ReadMetadata", BindingFlags.Instance | BindingFlags.NonPublic);
        var metadata = Assert.IsType<SaveMetadata>(method!.Invoke(reader, null));

        Assert.Equal("Windrose Test", metadata.ServerDescription?.ServerName);
        Assert.Equal("RocksDB block-based SST", metadata.CheckpointContainerFormat);
        Assert.Equal("Test World", metadata.WorldName);
        Assert.Equal("Custom", metadata.WorldPresetType);
        Assert.Equal(4, metadata.WorldSettingCount);
        Assert.Equal(2, metadata.WorldBoolSettingCount);
        Assert.Equal(1, metadata.WorldFloatSettingCount);
        Assert.Equal(1, metadata.WorldTagSettingCount);
        Assert.False(string.IsNullOrWhiteSpace(metadata.CheckpointExtractedPath));
        Assert.True(Directory.Exists(metadata.CheckpointExtractedPath));
        Assert.True(File.Exists(Path.Combine(metadata.CheckpointExtractedPath!, "AdditionalRecordFiles", "WorldDescription.json")));
        Assert.True(File.Exists(Path.Combine(metadata.CheckpointExtractedPath!, "AdditionalRecordFiles", "PlayerSnapshot.json")));
        Assert.Contains(metadata.CheckpointEntries, entry => entry.Path == "RocksDB/000123.sst");
        Assert.Contains(metadata.CheckpointEntries, entry => entry.Markers.Contains("R5BLPlayer"));
        Assert.Contains(metadata.CheckpointEntries, entry => entry.Markers.Contains("R5BLShip"));
        Assert.Contains(metadata.CheckpointEntries, entry => entry.ReadableTokens.Contains("R5BLPlayerInWorld"));
        Assert.Contains(metadata.CheckpointEntries, entry => entry.ReadableTokens.Contains("Blocks"));
        Assert.Contains(metadata.CheckpointEntries, entry => entry.ReadableTokens.Contains("DataKey"));
        Assert.Contains(metadata.CheckpointEntries, entry => entry.ReadableTokens.Contains("MarkupKey"));
        Assert.Contains(metadata.CheckpointEntries, entry => entry.ReadableTokens.Contains("IslandId"));
        Assert.Contains(metadata.CheckpointEntries, entry => entry.ReadableTokens.Contains("ShipId"));
        Assert.Contains(metadata.CheckpointEntries, entry => entry.ReadableTokens.Contains("ChangeRevision"));
        Assert.Contains(metadata.CheckpointEntries, entry => entry.ReadableTokens.Contains("WorldDescription"));
        Assert.Equal(2, metadata.DocumentSummaries.Count);
        Assert.Contains(metadata.DocumentSummaries, summary => summary.Kind == "world-description");
        Assert.Contains(metadata.CollectionSummaries, summary => summary.Name == "player");
        Assert.Contains(metadata.ObservedFamilies, family => family.Name == "ship-document" && family.Status == "not-observed");

        Directory.Delete(root, true);
    }

    [Fact]
    public void ReadsLowercaseWorldDescriptionIslandIdFromRealBackupShape()
    {
        var root = Path.Combine(Path.GetTempPath(), "windrose-state-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var serverFiles = Path.Combine(root, "server-files");
        var saveRoot = Path.Combine(serverFiles, "R5", "Saved", "SaveProfiles", "Default", "RocksDB_v2_Backups", "Worlds", "8D23C893C50A4DAF6390E4E698FC5C8E");
        Directory.CreateDirectory(saveRoot);

        File.WriteAllText(
            Path.Combine(serverFiles, "R5", "ServerDescription.json"),
            """
            {
              "ServerDescription_Persistent": {
                "ServerName": "Windrose Test",
                "WorldIslandId": "8D23C893C50A4DAF6390E4E698FC5C8E"
              }
            }
            """);

        var zipPath = Path.Combine(saveRoot, "8D23C893C50A4DAF6390E4E698FC5C8E_0.10.0_Latest.zip");
        using (var file = File.Create(zipPath))
        using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
        {
            AddEntry(
                archive,
                "AdditionalRecordFiles/WorldDescription.json",
                """
                {
                  "Version": 1,
                  "WorldDescription": {
                    "islandId": "8D23C893C50A4DAF6390E4E698FC5C8E",
                    "WorldName": "",
                    "WorldPresetType": "Medium",
                    "WorldSettings": {
                      "BoolParameters": {
                        "{\"TagName\": \"WDS.Parameter.EasyExplore\"}": false
                      },
                      "FloatParameters": {
                        "{\"TagName\": \"WDS.Parameter.MobHealthMultiplier\"}": 1
                      },
                      "TagParameters": {
                        "{\"TagName\": \"WDS.Parameter.CombatDifficulty\"}": {
                          "TagName": "WDS.Parameter.CombatDifficulty.Normal"
                        }
                      }
                    }
                  }
                }
                """);
        }

        var options = Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions
        {
            ServerFilesPath = serverFiles,
            SaveRootRelativePath = "R5/Saved/SaveProfiles/Default"
        });
        var reader = new SaveMetadataReader(options, new StubStateStore(), NullLogger<SaveMetadataReader>.Instance);

        var method = typeof(SaveMetadataReader).GetMethod("ReadMetadata", BindingFlags.Instance | BindingFlags.NonPublic);
        var metadata = Assert.IsType<SaveMetadata>(method!.Invoke(reader, null));

        Assert.Equal("8D23C893C50A4DAF6390E4E698FC5C8E", metadata.WorldIslandId);
        Assert.Equal("RocksDB block-based SST", metadata.CheckpointContainerFormat);
        Assert.Equal("Medium", metadata.WorldPresetType);
        Assert.Equal(3, metadata.WorldSettingCount);
        Assert.Contains(metadata.ObservedFamilies, family => family.Name == "player-in-world-metadata");

        Directory.Delete(root, true);
    }

    [Fact]
    public void ReportsAnErrorWhenNoLatestBackupExists()
    {
        var root = Path.Combine(Path.GetTempPath(), "windrose-state-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var serverFiles = Path.Combine(root, "server-files");
        Directory.CreateDirectory(Path.Combine(serverFiles, "R5"));
        File.WriteAllText(
            Path.Combine(serverFiles, "R5", "ServerDescription.json"),
            """
            {
              "ServerDescription_Persistent": {
                "ServerName": "Windrose Test",
                "WorldIslandId": "F3B27E1F83434AF5A1BBA9B40E848A42"
              }
            }
            """);

        var options = Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions
        {
            ServerFilesPath = serverFiles,
            SaveRootRelativePath = "R5/Saved/SaveProfiles/Default"
        });
        var reader = new SaveMetadataReader(options, new StubStateStore(), NullLogger<SaveMetadataReader>.Instance);

        var method = typeof(SaveMetadataReader).GetMethod("ReadMetadata", BindingFlags.Instance | BindingFlags.NonPublic);
        var metadata = Assert.IsType<SaveMetadata>(method!.Invoke(reader, null));

        Assert.Contains("No latest backup ZIP found", metadata.Error);
        Assert.Equal("Windrose Test", metadata.ServerDescription?.ServerName);
        Assert.Null(metadata.CheckpointContainerFormat);

        Directory.Delete(root, true);
    }

    private static void AddEntry(ZipArchive archive, string path, string contents)
    {
        var entry = archive.CreateEntry(path);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        writer.Write(contents);
    }

    private sealed class StubStateStore : IWindroseStateStore
    {
        public WindroseServerState GetState() => new();
        public void SetLogAvailable(bool available, string? error = null) { }
        public void Apply(WindroseEvent evt) { }
        public void UpdateSaveMetadata(SaveMetadata save) { }
        public ChannelReader<WindroseEvent> Subscribe(CancellationToken cancellationToken) => throw new NotSupportedException();
        public ChannelReader<WindroseStateChange> SubscribeStateChanges(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
