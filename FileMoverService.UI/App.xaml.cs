using System.Drawing;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Threading;
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

        // Elevated service-control helper: launched by the main instance via runas.
        // Performs the operation and exits — never shows a window.
        if (e.Args.Length == 3 && e.Args[0] == "--elevate" && e.Args[1] == "service")
        {
            RunElevatedServiceCommand(e.Args[2]);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, ex) =>
        {
            Logger.Log("Unhandled exception", ex.Exception);
            System.Windows.MessageBox.Show(
                $"Unexpected error: {ex.Exception.Message}\n\nDetails logged to:\n{Logger.LogFilePath}",
                "File Mover Service", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

        Logger.Log("Application started");
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.Icon = CreateTrayIcon();
    }

    private static void RunElevatedServiceCommand(string command)
    {
        try
        {
            using var sc = new ServiceController(FileMoverService.UI.MainWindow.ServiceName);
            switch (command)
            {
                case "start":
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                    break;
                case "stop":
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Elevated service command '{command}' failed", ex);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
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
