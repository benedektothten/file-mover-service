using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace FileMoverService.UI;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<WatchFolderEntry> _entries = [];

    public MainWindow()
    {
        InitializeComponent();
        _entries = new ObservableCollection<WatchFolderEntry>(ConfigService.Load());
        FolderList.ItemsSource = _entries;
    }

    private void AddRow_Click(object sender, RoutedEventArgs e) =>
        _entries.Add(new WatchFolderEntry());

    private void DeleteRow_Click(object sender, RoutedEventArgs e)
    {
        if (((System.Windows.Controls.Button)sender).Tag is WatchFolderEntry entry)
            _entries.Remove(entry);
    }

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        if (((System.Windows.Controls.Button)sender).Tag is WatchFolderEntry entry)
            entry.SourceFolder = BrowseFolder(entry.SourceFolder) ?? entry.SourceFolder;
    }

    private void BrowseTarget_Click(object sender, RoutedEventArgs e)
    {
        if (((System.Windows.Controls.Button)sender).Tag is WatchFolderEntry entry)
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
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ConfigService.Save(_entries);
        System.Windows.MessageBox.Show("Saved. Restart the service to apply changes.", "File Mover Service",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
