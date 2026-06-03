namespace FileMoverService;

public class AppSettings
{
    public required List<string> WatchFolders { get; init; }
    public required List<Rule> Rules { get; init; }
    
    public required List<string> TempExtensions { get; init; }

    public class Rule
    {
        public required string Extension { get; init; }
        public required string TargetFolder { get; init; }
    }
}