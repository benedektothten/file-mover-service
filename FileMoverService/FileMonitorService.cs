using Microsoft.Extensions.Options;

namespace FileMoverService;

public class FileMonitorService : BackgroundService
{
    private readonly ILogger<FileMonitorService> _logger;
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly AppSettings _settings;

    private readonly SemaphoreSlim _semaphore = new(2);

    public FileMonitorService(
        ILogger<FileMonitorService> logger,
        IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;

        foreach (var watchFolder in _settings.WatchFolders)
        {
            if (!Directory.Exists(watchFolder.SourceFolder))
            {
                _logger.LogWarning("Watch folder does not exist, skipping: {Folder}", watchFolder.SourceFolder);
                continue;
            }

            var watcher = new FileSystemWatcher(watchFolder.SourceFolder)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            watcher.Created += (s, e) => Task.Run(() => OnFileSystemEventAsync(s, e, watchFolder));
            watcher.Renamed += (s, e) => Task.Run(() => OnFileSystemEventAsync(s, e, watchFolder));

            _watchers.Add(watcher);
            _logger.LogInformation("Watching folder: {Source} -> {Target}", watchFolder.SourceFolder, watchFolder.TargetFolder);
        }
    }

    private async Task OnFileSystemEventAsync(object sender, FileSystemEventArgs e, AppSettings.WatchFolder watchFolder)
    {
        await _semaphore.WaitAsync();
        try
        {
            var fileInfo = new FileInfo(e.FullPath);

            if (IsTemporaryFile(fileInfo))
                return;

            if (!watchFolder.Extensions.Any(ext => ext.Equals(fileInfo.Extension, StringComparison.OrdinalIgnoreCase)))
                return;

            if (!Path.Exists(watchFolder.TargetFolder))
            {
                _logger.LogWarning("Target folder does not exist, skipping: {Folder}", watchFolder.TargetFolder);
                return;
            }

            await WaitForFileToBeUnlockedAsync(fileInfo);

            try
            {
                var targetPath = GenerateUniqueFileName(Path.Combine(watchFolder.TargetFolder, fileInfo.Name));
                File.Move(e.FullPath, targetPath);
                _logger.LogInformation("Moved {File} to {Target}", fileInfo.Name, watchFolder.TargetFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move file {File}", fileInfo.Name);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private bool IsTemporaryFile(FileInfo fileInfo) =>
        _settings.TempExtensions.Contains(fileInfo.Extension) || fileInfo.Length < 1024;

    private bool IsFileLocked(FileInfo file)
    {
        try
        {
            using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    private async Task WaitForFileToBeUnlockedAsync(FileInfo fileInfo)
    {
        while (IsFileLocked(fileInfo))
            await Task.Delay(1000);
    }

    private string GenerateUniqueFileName(string targetPath)
    {
        var directory = Path.GetDirectoryName(targetPath);
        var fileName = Path.GetFileNameWithoutExtension(targetPath);
        var extension = Path.GetExtension(targetPath);
        int count = 1;

        while (File.Exists(targetPath))
        {
            targetPath = Path.Combine(directory!, $"{fileName}_{count}{extension}");
            count++;
        }

        return targetPath;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
