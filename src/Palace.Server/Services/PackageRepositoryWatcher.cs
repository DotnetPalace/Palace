using System.Collections.Concurrent;

using Microsoft.AspNetCore.Identity;

namespace Palace.Server.Services;

public class PackageRepositoryWatcher : BackgroundService
{
    private readonly Configuration.GlobalSettings _settings;
    private readonly Orchestrator _orchestrator;
    private readonly ILogger<PackageRepositoryWatcher> _logger;

    private ConcurrentDictionary<string, DateTime> _uploadedFiles = new();

    public PackageRepositoryWatcher(Configuration.GlobalSettings settings,
        Services.Orchestrator orchestrator,
        ILogger<PackageRepositoryWatcher> logger)
    {
        _settings = settings;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    protected FileSystemWatcher Watcher { get; set; } = default!;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_settings.StagingFolder))
        {
            return base.StartAsync(cancellationToken);
        }
        Watcher = new FileSystemWatcher(_settings.StagingFolder);
        Watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess;
        Watcher.Changed += OnChanged;
        Watcher.Created += OnChanged;
        Watcher.EnableRaisingEvents = true;

        return base.StartAsync(cancellationToken);
    }

    private async void OnChanged(object sender, FileSystemEventArgs args)
    {
        _logger.LogDebug("Detect file {ChangeType} {FullPath}", args.FullPath, args.ChangeType);

        if (args.Name!.EndsWith(".tmp", StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.LogTrace("detect temp file {0} {1}", args.Name, args.ChangeType);
            return;
        }

        // Filtrer sur les zip uniquement
        if (args.Name.IndexOf(".zip", StringComparison.InvariantCultureIgnoreCase) == -1)
        {
            _logger.LogTrace("detect {0} {1} not zip file", args.Name, args.ChangeType);
            return;
        }

        var unlock = await WaitForUnlock(args.FullPath);
        if (!unlock)
        {
            _logger.LogWarning("detect {0} file {1} not closed", args.Name, args.ChangeType);
            return;
        }

        _logger.LogInformation("File unlocked detected {ChangeType} {FullPath}", args.FullPath, args.ChangeType);

        var uploaded = _uploadedFiles.ContainsKey(args.FullPath);
        if (uploaded)
        {
            return;
        }

        try
        {
            _orchestrator.BackupAndUpdateRepositoryFile(args.FullPath);
            _uploadedFiles.TryAdd(args.FullPath, DateTime.Now);
        }
        catch (Exception ex)
        {
            ex.Data.Add("FullPath", args.FullPath);
            _logger.LogError(ex, ex.Message);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var expired = _uploadedFiles.Where(i => i.Value < DateTime.Now.AddSeconds(-30)).ToList();
            foreach (var item in expired)
			{
				_uploadedFiles.TryRemove(item.Key, out _);
			}
            await Task.Delay(1 * 1000, stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (Watcher != null)
        {
            Watcher.EnableRaisingEvents = false;
            Watcher.Changed -= OnChanged;
            Watcher.Created -= OnChanged;
            Watcher.Deleted -= OnChanged;
        }

        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        Watcher?.Dispose();
        base.Dispose();
    }

    public async Task<bool> WaitForUnlock(string fileName)
    {
        var loop = 0;
        bool success = false;
        while (true)
        {
            try
            {
                using var stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                if (stream.Length > 0)
                {
                    success = true;
                    break;
                }
            }
            catch (IOException)
            {
                _logger.LogDebug("Wait for unlock {0}", fileName);
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                ex.Data.Add("FileName", fileName);
                _logger.LogError(ex, ex.Message);
                await Task.Delay(500);
            }
            loop++;
            if (loop > 1000)
            {
                success = false;
                break;
            }
        }
        return success;
    }
}

