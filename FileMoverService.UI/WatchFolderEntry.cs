using System.ComponentModel;

namespace FileMoverService.UI;

public class WatchFolderEntry : INotifyPropertyChanged
{
    private string _sourceFolder = string.Empty;
    private string _targetFolder = string.Empty;
    private string _extensions = string.Empty;

    public string SourceFolder
    {
        get => _sourceFolder;
        set { _sourceFolder = value; OnPropertyChanged(nameof(SourceFolder)); }
    }

    public string TargetFolder
    {
        get => _targetFolder;
        set { _targetFolder = value; OnPropertyChanged(nameof(TargetFolder)); }
    }

    public string Extensions
    {
        get => _extensions;
        set { _extensions = value; OnPropertyChanged(nameof(Extensions)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
