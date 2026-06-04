using System.Collections;
using System.ComponentModel;
using System.IO;

namespace FileMoverService.UI;

public class WatchFolderEntry : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private string _sourceFolder = string.Empty;
    private string _targetFolder = string.Empty;
    private string _extensions = string.Empty;

    public string SourceFolder
    {
        get => _sourceFolder;
        set
        {
            _sourceFolder = value;
            OnPropertyChanged(nameof(SourceFolder));
            Validate(nameof(SourceFolder), value);
            ValidateSameFolder();
        }
    }

    public string TargetFolder
    {
        get => _targetFolder;
        set
        {
            _targetFolder = value;
            OnPropertyChanged(nameof(TargetFolder));
            Validate(nameof(TargetFolder), value);
            ValidateSameFolder();
        }
    }

    public string Extensions
    {
        get => _extensions;
        set { _extensions = value; OnPropertyChanged(nameof(Extensions)); }
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ── INotifyDataErrorInfo ──────────────────────────────────────────────────
    private readonly Dictionary<string, List<string>> _errors = [];

    public bool HasErrors => _errors.Count > 0;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (propertyName is null) return _errors.Values.SelectMany(e => e);
        return _errors.TryGetValue(propertyName, out var list) ? list : [];
    }

    private void Validate(string property, string value)
    {
        _errors.Remove(property);

        if (string.IsNullOrWhiteSpace(value))
            _errors[property] = ["Path cannot be empty."];
        else if (!Directory.Exists(value))
            _errors[property] = ["Folder does not exist."];

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
        OnPropertyChanged(nameof(HasErrors));
    }

    private void ValidateSameFolder()
    {
        const string key = nameof(TargetFolder);
        const string msg = "Source and target folders must be different.";

        var src = _sourceFolder?.Trim();
        var tgt = _targetFolder?.Trim();

        if (!string.IsNullOrWhiteSpace(src) && !string.IsNullOrWhiteSpace(tgt))
        {
            bool same;
            try { same = string.Equals(Path.GetFullPath(src), Path.GetFullPath(tgt), StringComparison.OrdinalIgnoreCase); }
            catch { same = string.Equals(src, tgt, StringComparison.OrdinalIgnoreCase); }

            if (same)
            {
                _errors[key] = [msg];
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(key));
                OnPropertyChanged(nameof(HasErrors));
                return;
            }
        }

        // Clear same-folder error if it was previously set (leave other errors intact)
        if (_errors.TryGetValue(key, out var list) && list.Contains(msg))
        {
            list.Remove(msg);
            if (list.Count == 0) _errors.Remove(key);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(key));
            OnPropertyChanged(nameof(HasErrors));
        }
    }
}
