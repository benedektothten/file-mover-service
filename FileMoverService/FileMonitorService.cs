using System.Text.Json;

namespace FileMoverService;

public class FileMonitorService : BackgroundService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "FileMoverService", "appsettings.json");

    private readonly ILogger<FileMonitorService> _logger;
    private readonly SemaphoreSlim _moveSemaphore = new(2);
    private readonly object _watcherLock = new();

    private List<FileSystemWatcher> _folderWatchers = [];
    private AppSettings _settings = new();

    public FileMonitorService(ILogger<FileMonitorService> logger)
    {
        _logger = logger;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Service starting. Config file: {Path}", ConfigPath);
        _logger.LogInformation("Config file exists: {Exists}", File.Exists(ConfigPath));

        ApplyConfig(LoadConfig());

        var lastWriteTime = File.Exists(ConfigPath)
            ? File.GetLastWriteTimeUtc(ConfigPath)
            : DateTime.MinValue;

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);

            if (!File.Exists(ConfigPath)) continue;

            var writeTime = File.GetLastWriteTimeUtc(ConfigPath);
            if (writeTime <= lastWriteTime) continue;

            lastWriteTime = writeTime;
            _logger.LogInformation("Config file changed at {Time}, reloading...", writeTime);
            ApplyConfig(LoadConfig());
        }
    }

    public override Task StopAsync(CancellationToken ct)
    {
        DisposeWatchers();
        return base.StopAsync(ct);
    }

    // ── Config ────────────────────────────────────────────────────────────────

    private AppSettings LoadConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            _logger.LogWarning("Config file not found at {Path}, running with no watch folders", ConfigPath);
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("AppSettings", out var section))
            {
                _logger.LogWarning("Config file has no AppSettings section");
                return new AppSettings();
            }

            return JsonSerializer.Deserialize<AppSettings>(
                section.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read config from {Path}", ConfigPath);
            return new AppSettings();
        }
    }

    private void ApplyConfig(AppSettings settings)
    {
        lock (_watcherLock)
        {
            _settings = settings;
            DisposeWatchers();
            _folderWatchers = [];

            foreach (var folder in settings.WatchFolders)
            {
                if (!Directory.Exists(folder.SourceFolder))
                {
                    _logger.LogWarning("Source folder not found, skipping: {Folder}", folder.SourceFolder);
                    continue;
                }

                var captured = folder;
                var watcher = new FileSystemWatcher(folder.SourceFolder)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };
                watcher.Created += (_, e) => _ = MoveFileAsync(e.FullPath, captured);
                watcher.Renamed += (_, e) => _ = MoveFileAsync(e.FullPath, captured);

                _folderWatchers.Add(watcher);
                _logger.LogInformation("Watching {Source} -> {Target}", folder.SourceFolder, folder.TargetFolder);
            }

            _logger.LogInformation("Config applied: {Count} folder(s) active", _folderWatchers.Count);
        }
    }

    private void DisposeWatchers()
    {
        foreach (var w in _folderWatchers)
        {
            w.EnableRaisingEvents = false;
            w.Dispose();
        }
    }

    // ── File moving ───────────────────────────────────────────────────────────

    private async Task MoveFileAsync(string filePath, AppSettings.WatchFolder folder)
    {
        await _moveSemaphore.WaitAsync();
        try
        {
            var file = new FileInfo(filePath);
            if (!file.Exists) return;

            AppSettings settings;
            lock (_watcherLock) settings = _settings;

            if (settings.TempExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)) return;
            if (file.Length < 1024) return;
            if (!folder.Extensions.Any(ext => ext.Equals(file.Extension, StringComparison.OrdinalIgnoreCase))) return;

            if (!Directory.Exists(folder.TargetFolder))
            {
                _logger.LogWarning("Target folder not found: {Folder}", folder.TargetFolder);
                return;
            }

            await WaitForFileReleaseAsync(file);

            var target = UniqueTargetPath(folder.TargetFolder, file.Name);
            File.Move(filePath, target);
            _logger.LogInformation("Moved {File} -> {Target}", file.Name, folder.TargetFolder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move {File}", filePath);
        }
        finally
        {
            _moveSemaphore.Release();
        }
    }

    private static async Task WaitForFileReleaseAsync(FileInfo file)
    {
        while (true)
        {
            try
            {
                using var s = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                return;
            }
            catch (IOException)
            {
                await Task.Delay(1000);
            }
        }
    }

    private static string UniqueTargetPath(string dir, string fileName)
    {
        var path = Path.Combine(dir, fileName);
        if (!File.Exists(path)) return path;

        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext  = Path.GetExtension(fileName);
        for (var i = 1; ; i++)
        {
            path = Path.Combine(dir, $"{name}_{i}{ext}");
            if (!File.Exists(path)) return path;
        }
    }
}
