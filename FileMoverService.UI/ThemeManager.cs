using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using Application = System.Windows.Application;
using Window = System.Windows.Window;

namespace FileMoverService.UI;

public static class ThemeManager
{
    // ── Win32 dark title bar ─────────────────────────────────────────────────
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    // ── Preference persistence ───────────────────────────────────────────────
    private static readonly string PrefsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FileMoverService", "ui-settings.json");

    public static bool IsDark { get; private set; }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Applies the saved user preference, or falls back to the OS setting.
    /// Must be called after the window's handle exists (Loaded or SourceInitialized).
    /// </summary>
    public static void Initialize(Window window)
    {
        IsDark = LoadPreference() ?? IsSystemDarkMode();
        Apply(window, IsDark, savePreference: false);
    }

    /// <summary>Toggles between light and dark and saves the preference.</summary>
    public static void Toggle(Window window)
    {
        Apply(window, !IsDark, savePreference: true);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private static void Apply(Window window, bool dark, bool savePreference)
    {
        IsDark = dark;

        // Swap theme resource dictionary
        var uri = new Uri($"Themes/{(dark ? "Dark" : "Light")}Theme.xaml", UriKind.Relative);
        var newDict = new ResourceDictionary { Source = uri };

        var dicts = Application.Current.Resources.MergedDictionaries;
        var existing = dicts.FirstOrDefault(d =>
            d.Source?.OriginalString.EndsWith("Theme.xaml") == true);
        if (existing != null) dicts.Remove(existing);
        dicts.Insert(0, newDict);   // insert at 0 so Controls.xaml DynamicResources resolve correctly

        // Dark title bar (Windows 10 build 18985+ / Windows 11)
        SetTitleBar(window, dark);

        // Update window background
        window.Background = (System.Windows.Media.Brush)Application.Current.Resources["WindowBackground"];

        if (savePreference) SavePreference(dark);
    }

    private static void SetTitleBar(Window window, bool dark)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            int value = dark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }
        catch { /* older Windows — not critical */ }
    }

    private static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch { return false; }
    }

    private static bool? LoadPreference()
    {
        try
        {
            if (!File.Exists(PrefsPath)) return null;
            var json = File.ReadAllText(PrefsPath);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("darkMode").GetBoolean();
        }
        catch { return null; }
    }

    private static void SavePreference(bool dark)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefsPath)!);
            File.WriteAllText(PrefsPath, JsonSerializer.Serialize(new { darkMode = dark }));
        }
        catch { /* best-effort */ }
    }
}
