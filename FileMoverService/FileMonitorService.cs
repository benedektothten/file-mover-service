using Microsoft.Extensions.Options;

namespace FileMoverService;

public class FileMonitorService : BackgroundService
{
    private readonly ILogger<FileMonitorService> _logger;
    private readonly FileSystemWatcher _watcher;
    private readonly AppSettings _settings;
    
    private readonly SemaphoreSlim _semaphore = new(2);

    public FileMonitorService(
        ILogger<FileMonitorService> logger,
        IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;

        _watcher = new FileSystemWatcher(_settings.DownloadFolder)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        _watcher.Created += (s, e) => Task.Run(() => OnFileSystemEventAsync(s, e));
        _watcher.Renamed += (s, e) => Task.Run(() => OnFileSystemEventAsync(s, e));

    }

    private async Task OnFileSystemEventAsync(object sender, FileSystemEventArgs e)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Additional check to ignore temporary files or system files
            var fileInfo = new FileInfo(e.FullPath);
    
            // Skip very small files or temporary files
            if (IsTemporaryFile(fileInfo))
                return;

            // Wait for the file to be fully downloaded
            await WaitForFileToBeUnlockedAsync(fileInfo);

            var rule = _settings.Rules.FirstOrDefault(r => 
                r.Extension.Equals(fileInfo.Extension, StringComparison.OrdinalIgnoreCase));

            if (rule != null)
            {
                try
                {
                    var targetPath = Path.Combine(rule.TargetFolder, fileInfo.Name);
                    targetPath = GenerateUniqueFileName(targetPath);
                    File.Move(e.FullPath, targetPath);

                    _logger.LogInformation($"Moved {fileInfo.Name} to {rule.TargetFolder}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to move file");
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Determines whether a given file is considered a temporary file or not.
    /// </summary>
    /// <param name="fileInfo">The <see cref="FileInfo"/> object representing the file to evaluate.</param>
    /// <returns>
    /// True if the file is identified as a temporary file (e.g., based on its extension or size); otherwise, false.
    /// </returns>
    private bool IsTemporaryFile(FileInfo fileInfo) => _settings.TempExtensions.Contains(fileInfo.Extension) || 
               fileInfo.Length < 1024;
    
    /// <summary>
    /// Checks whether a specified file is currently locked for access by another process.
    /// </summary>
    /// <param name="file">The <see cref="FileInfo"/> object representing the file to check.</param>
    /// <returns>
    /// True if the file is locked for access by another process; otherwise, false.
    /// </returns>
    private bool IsFileLocked(FileInfo file)
    {
        try
        {
            using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
            {
                return false;
            }
        }
        catch (IOException)
        {
            return true;
        }
    }

    /// <summary>
    /// Waits asynchronously until the specified file is no longer locked for access by another process.
    /// </summary>
    /// <param name="fileInfo">The <see cref="FileInfo"/> object representing the file to check.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    private async Task WaitForFileToBeUnlockedAsync(FileInfo fileInfo)
    {
        while (IsFileLocked(fileInfo))
        {
            await Task.Delay(1000); // Use async wait instead of blocking the thread
        }
    }

    /// <summary>
    /// Generates a unique file name by appending an incremental suffix to avoid conflicts with existing files.
    /// </summary>
    /// <param name="targetPath">The full path of the intended file, including its name and extension.</param>
    /// <returns>
    /// A unique file path with a modified file name if a file with the same name already exists in the target location.
    /// </returns>
    private string GenerateUniqueFileName(string targetPath)
    {
        var directory = Path.GetDirectoryName(targetPath);
        var fileName = Path.GetFileNameWithoutExtension(targetPath);
        var extension = Path.GetExtension(targetPath);
        int count = 1;

        while (File.Exists(targetPath))
        {
            targetPath = Path.Combine(directory, $"{fileName}_{count}{extension}");
            count++;
        }

        return targetPath;
    }


    /// <summary>
    /// Executes the main background processing loop for the service.
    /// </summary>
    /// <param name="stoppingToken">A <see cref="CancellationToken"/> that signals when the operation should be stopped.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the background operation.
    /// </returns>
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}