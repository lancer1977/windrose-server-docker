using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Tests.Domain;

public sealed class SaveDocumentSummaryDefaultsTests
{
    [Fact]
    public void SaveDocumentSummary_UsesDefaultValues_WhenNotConfigured()
    {
        var summary = new SaveDocumentSummary();

        Assert.Equal(string.Empty, summary.Path);
        Assert.Equal(string.Empty, summary.Kind);
        Assert.Null(summary.SizeBytes);
        Assert.Equal(0, summary.ScalarPropertyCount);
        Assert.Equal(0, summary.ObjectCount);
        Assert.Equal(0, summary.ArrayCount);
        Assert.Empty(summary.ScalarPreview);
        Assert.Null(summary.Notes);
    }

    [Fact]
    public void SaveDocumentSummary_PreservesConfiguredValues()
    {
        var summary = new SaveDocumentSummary
        {
            Path = "/world/save.json",
            Kind = "JSON",
            SizeBytes = 12_345L,
            ScalarPropertyCount = 5,
            ObjectCount = 2,
            ArrayCount = 7,
            ScalarPreview = new Dictionary<string, string> { ["seed"] = "high" },
            Notes = "loaded"
        };

        Assert.Equal("/world/save.json", summary.Path);
        Assert.Equal("JSON", summary.Kind);
        Assert.Equal(12_345L, summary.SizeBytes);
        Assert.Equal(5, summary.ScalarPropertyCount);
        Assert.Equal(2, summary.ObjectCount);
        Assert.Equal(7, summary.ArrayCount);
        Assert.Equal("high", summary.ScalarPreview["seed"]);
        Assert.Equal("loaded", summary.Notes);
    }
}
