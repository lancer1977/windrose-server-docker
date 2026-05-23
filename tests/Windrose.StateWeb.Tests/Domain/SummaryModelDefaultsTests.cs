using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Tests.Domain;

public sealed class SummaryModelDefaultsTests
{
    [Fact]
    public void CheckpointAndObservedSummary_HaveSafeEmptyDefaults()
    {
        var checkpoint = new CheckpointEntrySummary();
        var observed = new ObservedFamilySummary();
        var collection = new SaveCollectionSummary();
        var document = new SaveDocumentSummary();

        Assert.Equal("", checkpoint.Path);
        Assert.Equal(0L, checkpoint.SizeBytes);
        Assert.Equal("", checkpoint.Kind);
        Assert.Empty(checkpoint.Markers);
        Assert.Empty(checkpoint.ReadableTokens);

        Assert.Equal("", observed.Name);
        Assert.Equal("", observed.Status);
        Assert.Equal("", observed.Notes);
        Assert.Empty(observed.Evidence);

        Assert.Equal("", collection.Name);
        Assert.Equal(0, collection.Count);
        Assert.Equal(0L, collection.TotalBytes);

        Assert.Equal("", document.Path);
        Assert.Equal("", document.Kind);
        Assert.Null(document.SizeBytes);
        Assert.Equal(0, document.ScalarPropertyCount);
        Assert.Equal(0, document.ObjectCount);
        Assert.Equal(0, document.ArrayCount);
        Assert.NotNull(document.ScalarPreview);
        Assert.Empty(document.ScalarPreview);
        Assert.Null(document.Notes);
    }
}

