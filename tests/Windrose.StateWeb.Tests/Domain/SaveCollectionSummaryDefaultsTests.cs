using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Tests.Domain;

public sealed class SaveCollectionSummaryDefaultsTests
{
    [Fact]
    public void SaveCollectionSummary_UsesDefaultValues_WhenNotConfigured()
    {
        var summary = new SaveCollectionSummary();

        Assert.Equal(string.Empty, summary.Name);
        Assert.Equal(0, summary.Count);
        Assert.Equal(0L, summary.TotalBytes);
    }

    [Fact]
    public void SaveCollectionSummary_PreservesConfiguredValues()
    {
        var summary = new SaveCollectionSummary
        {
            Name = "players",
            Count = 42,
            TotalBytes = 99_999L
        };

        Assert.Equal("players", summary.Name);
        Assert.Equal(42, summary.Count);
        Assert.Equal(99_999L, summary.TotalBytes);
    }
}
