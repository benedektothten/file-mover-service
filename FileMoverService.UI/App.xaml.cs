using System.Drawing;
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
        _trayIcon.Icon = CreateTrayIcon();
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

    private static Icon CreateTrayIcon()
    {
        var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.FillEllipse(new SolidBrush(Color.FromArgb(0, 120, 215)), 1, 1, 30, 30);
        g.DrawString("F", new Font("Segoe UI", 14, System.Drawing.FontStyle.Bold), Brushes.White,
            new RectangleF(0, 4, 32, 28),
            new StringFormat { Alignment = StringAlignment.Center });
        return Icon.FromHandle(bitmap.GetHicon());
    }

    private void TrayOpen_Click(object sender, RoutedEventArgs e) => ShowMainWindow();
    private void TrayExit_Click(object sender, RoutedEventArgs e) => Shutdown();
}
