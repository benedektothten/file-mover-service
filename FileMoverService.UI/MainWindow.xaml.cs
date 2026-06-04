using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Forms;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace FileMoverService.UI;

public partial class MainWindow : Window
{
    internal const string ServiceName = "FileMoverService";
    private const string BaseTitle = "File Mover Service";
    private readonly ObservableCollection<WatchFolderEntry> _entries;
    private bool _isDirty;

    public MainWindow()
    {
        InitializeComponent();
        _entries = new ObservableCollection<WatchFolderEntry>(ConfigService.Load());
        FolderList.ItemsSource = _entries;

        _entries.CollectionChanged += OnEntriesChanged;
        foreach (var e in _entries) e.PropertyChanged += OnEntryPropertyChanged;

        RefreshServiceButtons();
    }

    // ── Dirty tracking ────────────────────────────────────────────────────────

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (WatchFolderEntry entry in e.NewItems)
                entry.PropertyChanged += OnEntryPropertyChanged;

        if (e.OldItems != null)
            foreach (WatchFolderEntry entry in e.OldItems)
                entry.PropertyChanged -= OnEntryPropertyChanged;

        MarkDirty();
    }

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e) => MarkDirty();

    private void MarkDirty()
    {
        _isDirty = true;
        Title = BaseTitle + " *";
    }

    private void ClearDirty()
    {
        _isDirty = false;
        Title = BaseTitle;
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void AddRow_Click(object sender, RoutedEventArgs e) =>
        _entries.Add(new WatchFolderEntry());

    private void DeleteRow_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is WatchFolderEntry entry)
            _entries.Remove(entry);
    }

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is WatchFolderEntry entry)
            entry.SourceFolder = BrowseFolder(entry.SourceFolder) ?? entry.SourceFolder;
    }

    private void BrowseTarget_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is WatchFolderEntry entry)
            entry.TargetFolder = BrowseFolder(entry.TargetFolder) ?? entry.TargetFolder;
    }

    private static string? BrowseFolder(string initialPath)
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = initialPath,
            UseDescriptionForTitle = true,
            Description = "Select folder"
        };
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Logger.Log($"Save clicked. Config path: {ConfigService.ConfigPath}");
            ConfigService.Save(_entries);
            Logger.Log("Save succeeded");
            ClearDirty();
            MessageBox.Show("Configuration saved.", "File Mover Service",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Log("Save failed", ex);
            MessageBox.Show($"Failed to save: {ex.Message}\n\nDetails logged to:\n{Logger.LogFilePath}",
                "File Mover Service", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ToggleService_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            var command = sc.Status == ServiceControllerStatus.Running ? "stop" : "start";

            // Start/stop requires admin rights. Relaunch ourselves elevated for just
            // this operation — UAC shows our own app name rather than cmd.exe.
            var exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule!.FileName;
            var psi = new ProcessStartInfo(exe, $"--elevate service {command}")
            {
                Verb = "runas",
                UseShellExecute = true
            };

            ServiceButton.IsEnabled = false;
            var process = Process.Start(psi);
            process?.WaitForExit(20_000);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled the UAC prompt — silently ignore
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not change service state: {ex.Message}", "File Mover Service",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            RefreshServiceButtons();
        }
    }

    private void RefreshServiceButtons()
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            var running = sc.Status == ServiceControllerStatus.Running;
            ServiceButton.IsEnabled = true;
            ServiceIndicator.Fill = running
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;
            ServiceLabel.Text = running ? "Stop Service" : "Start Service";
        }
        catch
        {
            ServiceButton.IsEnabled = false;
            ServiceIndicator.Fill = System.Windows.Media.Brushes.Gray;
            ServiceLabel.Text = "Service unavailable";
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) =>
        ThemeManager.Initialize(this);

    private void ThemeToggle_Click(object sender, RoutedEventArgs e) =>
        ThemeManager.Toggle(this);

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (_isDirty)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Save before closing?",
                "File Mover Service",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (result == MessageBoxResult.Yes)
                Save_Click(sender, new RoutedEventArgs());
        }

        e.Cancel = true;
        Hide();
    }
}
