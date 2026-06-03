using System.Collections.ObjectModel;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Forms;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace FileMoverService.UI;

public partial class MainWindow : Window
{
    private const string ServiceName = "TorrentMoverService";
    private readonly ObservableCollection<WatchFolderEntry> _entries;

    public MainWindow()
    {
        InitializeComponent();
        _entries = new ObservableCollection<WatchFolderEntry>(ConfigService.Load());
        FolderList.ItemsSource = _entries;
        RefreshServiceButtons();
    }

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
        ConfigService.Save(_entries);
        MessageBox.Show("Saved. Restart the service to apply changes.", "File Mover Service",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ToggleService_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
            }
            else
            {
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not change service state: {ex.Message}", "File Mover Service",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        RefreshServiceButtons();
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

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
