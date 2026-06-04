namespace FileMoverService;

public class AppSettings
{
    public List<WatchFolder> WatchFolders { get; init; } = [];
    public List<string> TempExtensions { get; init; } = [];

    public class WatchFolder
    {
        public required string SourceFolder { get; init; }
        public required string TargetFolder { get; init; }
        public required List<string> Extensions { get; init; }
    }
}
