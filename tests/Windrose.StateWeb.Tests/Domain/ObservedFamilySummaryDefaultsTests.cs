using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Tests.Domain;

public sealed class ObservedFamilySummaryDefaultsTests
{
    [Fact]
    public void ObservedFamilySummary_UsesDefaultValues_WhenNotConfigured()
    {
        var summary = new ObservedFamilySummary();

        Assert.Equal(string.Empty, summary.Name);
        Assert.Equal(string.Empty, summary.Status);
        Assert.Equal(string.Empty, summary.Notes);
        Assert.Empty(summary.Evidence);
    }

    [Fact]
    public void ObservedFamilySummary_PreservesConfiguredValues()
    {
        var summary = new ObservedFamilySummary
        {
            Name = "Lupine",
            Status = "Stable",
            Notes = "No issues",
            Evidence = ["spawned", "active"]
        };

        Assert.Equal("Lupine", summary.Name);
        Assert.Equal("Stable", summary.Status);
        Assert.Equal("No issues", summary.Notes);
        Assert.Equal(new[] { "spawned", "active" }, summary.Evidence);
    }
}
