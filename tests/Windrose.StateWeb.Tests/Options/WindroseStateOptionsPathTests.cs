using Windrose.StateWeb.Options;

namespace Windrose.StateWeb.Tests.Options;

public sealed class WindroseStateOptionsPathTests
{
    [Fact]
    public void ResolvesAllCoreFilePaths()
    {
        var options = new WindroseStateOptions
        {
            ServerFilesPath = "/srv/windrose",
            LogRelativePath = "R5/log.txt",
            SaveRootRelativePath = "R5/Saves",
            ServerDescriptionRelativePath = "R5/desc.json"
        };

        Assert.Equal("/srv/windrose/R5/log.txt", options.LogPath);
        Assert.Equal("/srv/windrose/R5/Saves", options.SaveRootPath);
        Assert.Equal("/srv/windrose/R5/desc.json", options.ServerDescriptionPath);
    }
}

