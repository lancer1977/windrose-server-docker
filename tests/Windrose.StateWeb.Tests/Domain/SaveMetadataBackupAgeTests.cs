using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Tests.Domain;

public sealed class SaveMetadataBackupAgeTests
{
    [Fact]
    public void BackupAge_ReturnsNull_WhenLatestBackupTimeMissing()
    {
        var metadata = new SaveMetadata();
        Assert.Null(metadata.BackupAge);
    }

    [Fact]
    public void BackupAge_CalculatesOffsetFromLatestBackupTime()
    {
        var metadata = new SaveMetadata
        {
            LatestBackupTime = new DateTimeOffset(2026, 3, 3, 0, 0, 0, TimeSpan.Zero)
        };

        var age = metadata.BackupAge;
        Assert.NotNull(age);
        Assert.True(age.Value.TotalMilliseconds >= 0);
    }
}

