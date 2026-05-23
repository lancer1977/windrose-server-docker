using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Tests.Domain;

public sealed class CheckpointEntrySummaryDefaultsTests
{
    [Fact]
    public void CheckpointEntrySummary_UsesDefaultValues_WhenNotConfigured()
    {
        var checkpoint = new CheckpointEntrySummary();

        Assert.Equal(string.Empty, checkpoint.Path);
        Assert.Equal(0L, checkpoint.SizeBytes);
        Assert.Equal(string.Empty, checkpoint.Kind);
        Assert.Empty(checkpoint.Markers);
        Assert.Empty(checkpoint.ReadableTokens);
    }

    [Fact]
    public void CheckpointEntrySummary_PreservesConfiguredValues()
    {
        var checkpoint = new CheckpointEntrySummary
        {
            Path = "/tmp/world",
            SizeBytes = 12_345L,
            Kind = "Checkpoint",
            Markers = ["verified", "latest"],
            ReadableTokens = ["abc", "def"]
        };

        Assert.Equal("/tmp/world", checkpoint.Path);
        Assert.Equal(12_345L, checkpoint.SizeBytes);
        Assert.Equal("Checkpoint", checkpoint.Kind);
        Assert.Equal(new[] { "verified", "latest" }, checkpoint.Markers);
        Assert.Equal(new[] { "abc", "def" }, checkpoint.ReadableTokens);
    }
}
