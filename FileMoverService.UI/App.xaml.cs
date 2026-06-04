using System.Diagnostics;
using System.Drawing;
using System.ServiceProcess;
using System.Windows;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using Separator = System.Windows.Controls.Separator;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Application = System.Windows.Application;

namespace FileMoverService.UI;

public partial class App : Application
{
    private TaskbarIcon _trayIcon = null!;
    private MainWindow? _mainWindow;
    private MenuItem _serviceMenuItem = null!;

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
        _trayIcon.ContextMenu = BuildContextMenu();
        ShowMainWindow();
    }

    // ── Context menu ──────────────────────────────────────────────────────────

    private ContextMenu BuildContextMenu()
    {
        var openItem = new MenuItem { Header = "Open", FontWeight = System.Windows.FontWeights.Bold };
        openItem.Click += (_, _) => ShowMainWindow();

        _serviceMenuItem = new MenuItem();
        _serviceMenuItem.Click += (_, _) => TrayToggleService();

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => Shutdown();

        var menu = new ContextMenu();
        menu.Items.Add(openItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(_serviceMenuItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);

        // Refresh service status every time the menu opens
        menu.Opened += (_, _) => RefreshServiceMenuItem();

        return menu;
    }

    private void RefreshServiceMenuItem()
    {
        try
        {
            using var sc = new ServiceController(FileMoverService.UI.MainWindow.ServiceName);
            _serviceMenuItem.IsEnabled = true;
            _serviceMenuItem.Header = sc.Status == ServiceControllerStatus.Running
                ? "Stop Service" : "Start Service";
        }
        catch
        {
            _serviceMenuItem.Header = "Service Unavailable";
            _serviceMenuItem.IsEnabled = false;
        }
    }

    private void TrayToggleService()
    {
        try
        {
            using var sc = new ServiceController(FileMoverService.UI.MainWindow.ServiceName);
            var command = sc.Status == ServiceControllerStatus.Running ? "stop" : "start";
            var exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule!.FileName;
            var psi = new ProcessStartInfo(exe, $"--elevate service {command}")
            {
                Verb = "runas",
                UseShellExecute = true
            };
            Process.Start(psi)?.WaitForExit(20_000);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled UAC — ignore
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Could not change service state: {ex.Message}",
                "File Mover Service", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Refresh the main window indicator if it is open
        _mainWindow?.RefreshServiceStatus();

    }

    // ── Elevated helper ───────────────────────────────────────────────────────

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

    // ── Window management ─────────────────────────────────────────────────────

    internal void ShowMainWindow()
    {
        if (_mainWindow == null || !_mainWindow.IsLoaded)
            _mainWindow = new MainWindow();

        // Always Show() — handles both first open and re-show after Hide()
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }

    // ── Tray icon ─────────────────────────────────────────────────────────────

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
}
