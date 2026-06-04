using System.IO;

namespace FileMoverService.UI;

internal static class Logger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FileMoverService", "ui.log");

    static Logger()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
    }

    internal static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { /* never let logging crash the app */ }
    }

    internal static void Log(string message, Exception ex) =>
        Log($"{message}: {ex}");

    internal static string LogFilePath => LogPath;
}
