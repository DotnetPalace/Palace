using System.Collections.Concurrent;
using System.Diagnostics;

using ArianeBus;

using Microsoft.Extensions.Caching.Memory;

using Palace.Server.Models;

namespace Palace.Server.Services;

public class Orchestrator
{
    private readonly ConcurrentDictionary<string, Models.ExtendedMicroServiceInfo> _extendedMicroServiceInfoList = new();
    private readonly ConcurrentDictionary<string, Models.HostInfo> _hostInfoList = new();
    private readonly ConcurrentDictionary<string, Models.PackageInfo> _packageList = new();

    private readonly ILogger<Orchestrator> _logger;
    private readonly Configuration.GlobalSettings _settings;
	private readonly IServiceBus _bus;
	private readonly LongActionService _longActionService;

	public event Action<Models.PackageInfo> OnPackageChanged = default!;
    public event Action<Models.HostInfo> OnHostChanged = default!;
    public event Action<Models.ExtendedMicroServiceInfo> OnServiceChanged = default!;

    public Orchestrator(ILogger<Orchestrator> logger,
        Configuration.GlobalSettings settings,
        ArianeBus.IServiceBus bus,
        LongActionService longActionService)
    {
        _logger = logger;
        _settings = settings;
		_bus = bus;
		_longActionService = longActionService;
		LoadPackageList();
    }

    public IEnumerable<Models.PackageInfo> GetPackageInfoList()
    {
        return _packageList.Select(i => i.Value);
    }

    public IEnumerable<Models.ExtendedMicroServiceInfo> GetServiceList()
    {
        return _extendedMicroServiceInfoList.Select(i => i.Value);
    }

    public IEnumerable<Models.HostInfo> GetHostList()
    {
        return _hostInfoList.Select(i => i.Value);
    }

    public void BackupAndUpdateRepositoryFile(string zipFileFullPath)
    {
        var zipFileName = Path.GetFileName(zipFileFullPath.ToLower());

        // Prise en compte du pattern filename.zip.version.*
        var parts = zipFileName.Split('.').ToList();
        string? version = null;
        var index = parts.IndexOf("zip");
        version = string.Join(".", parts.Skip(index + 1).Take(int.MaxValue));
        if (!string.IsNullOrWhiteSpace(version))
        {
            zipFileName = zipFileName.Replace($".{version}", "");
        }

        _logger.LogInformation("BackupAndUpdateRepositoryFile {0} with version {1} zipName {2}", zipFileFullPath, version, zipFileName);
        var list = GetPackageInfoList();

        var availablePackage = list.FirstOrDefault(i => i.PackageFileName.Equals(zipFileName, StringComparison.InvariantCultureIgnoreCase));
        if (availablePackage != null)
        {
            if (availablePackage.ChangeDetected)
            {
                _logger.LogInformation("BackupAndUpdateRepositoryFile {0} with version {1} change already detected", zipFileFullPath, version);
            }
            availablePackage.ChangeDetected = true;
        }

        _logger.LogInformation("Start BackupAndUpdateRepositoryFile {0} with version {1}", zipFileFullPath, version);

        var destFileName = Path.Combine(_settings.RepositoryFolder, zipFileName);
        if (File.Exists(destFileName))
        {
            // Backup
            string backupDirectory = _settings.BackupFolder;
            if (string.IsNullOrWhiteSpace(version))
            {
                _logger.LogInformation("Try to BackupAndUpdateRepositoryFile {0}", zipFileFullPath);
                backupDirectory = GetNewBackupDirectory(zipFileName);
                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }
                var backupFileName = Path.Combine(backupDirectory, zipFileName);
                File.Copy(zipFileFullPath, backupFileName, true);
                _logger.LogInformation("Backup from {0} to {1} ", zipFileFullPath, backupFileName);
            }
            else
            {
                _logger.LogInformation("Try to BackupAndUpdateRepositoryFile {0} with version {1}", zipFileFullPath, version);
                var directoryPart = zipFileName.Replace(".zip", "", StringComparison.InvariantCultureIgnoreCase);
                var existingBackupFileName = Path.Combine(backupDirectory, directoryPart, version, zipFileName);
                if (File.Exists(existingBackupFileName))
                {
                    _logger.LogInformation("File {0} with version {1} already backuped without changed", zipFileFullPath, version);
                    // Ne pas faire de mise à jour
                    return;
                }
                var destDirectory = Path.GetDirectoryName(existingBackupFileName)!;
                if (!Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }
                var backupFileName = Path.Combine(destDirectory, zipFileName);
                _logger.LogInformation("Try to Backup from {0} to {1} ", zipFileFullPath, backupFileName);
                File.Copy(zipFileFullPath, backupFileName, true);
                _logger.LogInformation("Backup from {0} to {1} ", zipFileFullPath, backupFileName);
            }
        }

        try
        {
            _logger.LogInformation("Try to deploy {0} to {1} ", zipFileFullPath, destFileName);
            File.Copy(zipFileFullPath, destFileName, true);
            _logger.LogInformation("package {0} deployed", destFileName);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "deploy {0} failed", destFileName);
            return;
        }
        finally
        {
            if (availablePackage != null)
            {
                availablePackage.ChangeDetected = false;
            }
        }

        if (availablePackage is not null)
        {
            LoadPackageList();
            OnPackageChanged?.Invoke(availablePackage);
        }
    }

    public void AddOrUpdateHost(Models.HostInfo hostInfo)
    {
        _hostInfoList.TryGetValue(hostInfo.HostName, out var existing);
        if (existing is null)
        {
            existing = hostInfo;
            _hostInfoList.TryAdd(hostInfo.HostName, hostInfo);
        }

        existing.LastHitDate = DateTime.Now;
        existing.ExternalIp = hostInfo.ExternalIp;
        existing.Version = hostInfo.Version;
        existing.HostState = hostInfo.HostState;
        existing.MainFileName = hostInfo.MainFileName;
        existing.TotalDriveSize = hostInfo.TotalDriveSize;
        existing.TotalFreeSpaceOfDriveSize = hostInfo.TotalFreeSpaceOfDriveSize;
        existing.OsDescription = hostInfo.OsDescription;
        existing.OsVersion = hostInfo.OsVersion;
        existing.ProcessId = hostInfo.ProcessId;
        existing.PercentCpu = hostInfo.PercentCpu;

		OnHostChanged?.Invoke(hostInfo);
    }

    public Models.ExtendedMicroServiceInfo? GetExtendedMicroServiceInfoByKey(string key)
    {
        _extendedMicroServiceInfoList.TryGetValue(key, out var result);
		return result;
    }

    public void AddOrUpdateMicroServiceInfo(Models.ExtendedMicroServiceInfo microserviceInfo)
    {
        _extendedMicroServiceInfoList.TryGetValue(microserviceInfo.Key, out var emsi);
        if (emsi is null)
        {
            emsi = new Models.ExtendedMicroServiceInfo();
            emsi.HostName = microserviceInfo.HostName;
            emsi.ServiceName = microserviceInfo.ServiceName;
            _extendedMicroServiceInfoList.TryAdd(emsi.Key, emsi);
        }

        emsi!.Location = microserviceInfo.Location;
        emsi.UserInteractive = microserviceInfo.UserInteractive;
        if (string.IsNullOrWhiteSpace(microserviceInfo.Version))
        {
            emsi.Version = microserviceInfo.Version;
        }
        emsi.LastWriteTime = microserviceInfo.LastWriteTime;
        if (microserviceInfo.ThreadCount > 0)
        {
            emsi.ThreadCount = microserviceInfo.ThreadCount;
            emsi.ThreadCountHistory.Add(new PerformanceCounter
            {
                Value = microserviceInfo.ThreadCount,
            });
            if (emsi.ThreadCountHistory.Count > 100)
            {
                emsi.ThreadCountHistory.RemoveAt(0);
            }
        }
        emsi.ProcessId = microserviceInfo.ProcessId;
        emsi.ServiceState = microserviceInfo.ServiceState;
        emsi.StartedDate = microserviceInfo.StartedDate;
        emsi.CommandLine = microserviceInfo.CommandLine;
        emsi.PeakPagedMem = microserviceInfo.PeakPagedMem;
        emsi.PeakVirtualMem = microserviceInfo.PeakVirtualMem;
        emsi.PeakWorkingSet = microserviceInfo.PeakWorkingSet;
        if (microserviceInfo.WorkingSet > 0)
        {
            emsi.WorkingSet = microserviceInfo.WorkingSet;
            emsi.WorkingSetHistory.Add(new PerformanceCounter
			{
				Value = microserviceInfo.WorkingSet,
			});
            if (emsi.WorkingSetHistory.Count > 100)
			{
				emsi.WorkingSetHistory.RemoveAt(0);
			}
        }
        emsi.CommandLine = microserviceInfo.CommandLine;
        emsi.EnvironmentName = microserviceInfo.EnvironmentName;
        emsi.LastHitDate = microserviceInfo.LastHitDate;
        emsi.Log = microserviceInfo.Log;
        emsi.FailReason = microserviceInfo.FailReason;

        OnServiceChanged?.Invoke(emsi);
    }

    public async Task<string?> RemovePackage(string packageFileName)
    {
        var fileName = Path.Combine(_settings.RepositoryFolder, packageFileName);
        if (File.Exists(fileName))
        {
            try
            {
                File.Delete(fileName);
                await Task.Delay(1000);
                LoadPackageList();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        return null;
    }

    public List<FileInfo> GetBackupFileList(string packageFileName)
    {
        var directoryPart = packageFileName.Replace(".zip", "", StringComparison.InvariantCultureIgnoreCase);
        var backupDirectory = Path.Combine(_settings.BackupFolder, directoryPart);

        if (!Directory.Exists(backupDirectory))
        {
            return new List<FileInfo>();
        }
        var list = from f in Directory.GetFiles(backupDirectory, "*.*", SearchOption.AllDirectories)
                   let fileInfo = new FileInfo(f)
                   select fileInfo;

        var result = list.OrderByDescending(i => i.CreationTime).ToList();
        return result;
    }

    public string? RollbackPackage(Models.PackageInfo package, FileInfo fileInfo)
    {
        var destPackage = Path.Combine(_settings.RepositoryFolder, package.PackageFileName);
        try
        {
            fileInfo.LastWriteTime = DateTime.Now;
            fileInfo.CreationTime = DateTime.Now;
            File.Copy(fileInfo.FullName, destPackage, true);
            LoadPackageList();
            OnPackageChanged?.Invoke(package);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return null;
    }

    public void GlobalReset()
    {
        _extendedMicroServiceInfoList.Clear();
        _hostInfoList.Clear();
        _packageList.Clear();
        LoadPackageList();
		_bus.PublishTopic(_settings.ServerResetTopicName, new Palace.Shared.Messages.ServerReset());
        OnHostChanged?.Invoke(new HostInfo());
	}

	private void LoadPackageList()
	{
		_packageList.Clear();
		var zipFileList = from f in Directory.GetFiles(_settings.RepositoryFolder, "*.zip", SearchOption.AllDirectories)
						  let fileInfo = new FileInfo(f)
						  select fileInfo;

		foreach (var item in zipFileList)
		{
			var info = new Models.PackageInfo
			{
				PackageFileName = item.Name,
				LastWriteTime = item.LastWriteTime,
				Size = item.Length
			};
			SetCurrentVersion(info);
			_packageList.TryAdd(info.PackageFileName, info);
		}
	}

	private string GetNewBackupDirectory(string fileName)
    {
        var version = 1;
        var directoryPart = fileName.Replace(".zip", "", StringComparison.InvariantCultureIgnoreCase);

		var packageBackupDirectory = Path.Combine(_settings.BackupFolder, directoryPart);
        if (!Directory.Exists(packageBackupDirectory))
        {
            Directory.CreateDirectory(packageBackupDirectory);
        }
		var directoryList = Directory.GetDirectories(packageBackupDirectory);
        if (directoryList.Any())
        {
            var lastDirectory = directoryList.OrderByDescending(i => i).First();
			var parts = lastDirectory.Split(@"\");
			var versionString = parts[parts.Length - 1].Replace("v", "", StringComparison.InvariantCultureIgnoreCase);
			if (int.TryParse(versionString, out var lastVersion))
			{
				version = lastVersion + 1;
			}
        }

		string? backupDirectory = null;
        while (true)
        {
            backupDirectory = Path.Combine(_settings.BackupFolder, directoryPart, $"v{version}");
            if (Directory.Exists(backupDirectory))
            {
                version++;
                continue;
            }
            break;
        }
        return backupDirectory;
    }

    private void SetCurrentVersion(Models.PackageInfo availablePackage)
    {
        if (availablePackage == null)
        {
            return;
        }
        var backupList = GetBackupFileList(availablePackage.PackageFileName);
        if (backupList == null
            || !backupList.Any())
        {
            availablePackage.CurrentVersion = "unknown";
            return;
        }
        var lastBackup = backupList.First();
        var parts = lastBackup.FullName.Split(@"\");
        var version = parts[parts.Length - 2];
        availablePackage.CurrentVersion = version;
    }

	internal void RemoveMicroServiceInfo(ExtendedMicroServiceInfo rmi)
	{
        if (rmi is null) 
        {
            return;
		}

        _extendedMicroServiceInfoList.Remove(rmi.Key, out var existing);
		if (existing is not null)
		{
			OnServiceChanged?.Invoke(existing);
		}
	}
}
