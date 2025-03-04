namespace FileMoverService;

public class AppSettings
{
    public string DownloadFolder { get; set; }
    public List<Rule> Rules { get; set; }
    
    public List<string> TempExtensions { get; set; }

    public class Rule
    {
        public string Extension { get; set; }
        public string TargetFolder { get; set; }
    }
}