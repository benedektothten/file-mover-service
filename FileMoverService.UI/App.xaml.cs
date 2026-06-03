using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Application = System.Windows.Application;

namespace FileMoverService.UI;

public partial class App : Application
{
    private TaskbarIcon _trayIcon = null!;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        ShowMainWindow();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon.Dispose();
        base.OnExit(e);
    }

    internal void ShowMainWindow()
    {
        if (_mainWindow == null || !_mainWindow.IsLoaded)
        {
            _mainWindow = new MainWindow();
            _mainWindow.Show();
        }
        else
        {
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }
    }

    private void TrayOpen_Click(object sender, RoutedEventArgs e) => ShowMainWindow();
    private void TrayExit_Click(object sender, RoutedEventArgs e) => Shutdown();
}
