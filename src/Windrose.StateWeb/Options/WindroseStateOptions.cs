namespace Windrose.StateWeb.Options;

public sealed class WindroseStateOptions
{
    public int Port { get; set; } = 8781;
    public string ServerFilesPath { get; set; } = "/server-files";
    public string LogRelativePath { get; set; } = "R5/Saved/Logs/R5.log";
    public string SaveRootRelativePath { get; set; } = "R5/Saved/SaveProfiles/Default";
    public string SnapshotPath { get; set; } = "/tmp/windrose-state/current-state.json";
    public int EventRetention { get; set; } = 500;
    public int SaveMetadataPollSeconds { get; set; } = 30;
    public bool TailFromEnd { get; set; }

    public string LogPath => Path.Combine(ServerFilesPath, LogRelativePath);
    public string SaveRootPath => Path.Combine(ServerFilesPath, SaveRootRelativePath);
}
