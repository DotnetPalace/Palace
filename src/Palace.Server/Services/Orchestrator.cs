using Microsoft.Extensions.Caching.Memory;

using Palace.Server.Models;

namespace Palace.Server.Services;

public class Orchestrator
{
	private List<Models.ExtendedMicroServiceInfo>? _extendedMicroServiceInfoList = null;
    private List<Models.HostInfo>? _hostInfoList = null;
    private List<Models.PackageInfo>? _packageList = null;

	private readonly ILogger<Orchestrator> _logger;
    private readonly Configuration.GlobalSettings _settings;

    public event Action<Models.PackageInfo> OnPackageChanged = default!;
    public event Action<Models.HostInfo> OnHostChanged = default!;
    public event Action<Models.ExtendedMicroServiceInfo> OnServiceChanged = default!;

    public Orchestrator( ILogger<Orchestrator> logger,
        Configuration.GlobalSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public List<Models.PackageInfo> GetPackageInfoList()
    {
        if (_packageList != null)
		{
			return _packageList;
		}
        var result = new List<Models.PackageInfo>();

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
            result.Add(info);
        }
        _packageList = result;
        return result;
    }

    public List<Models.ExtendedMicroServiceInfo> GetServiceList()
    {
        if (_extendedMicroServiceInfoList is null)
        {
            _extendedMicroServiceInfoList = new();
        }
        return _extendedMicroServiceInfoList;
    }

    public List<Models.HostInfo> GetHostList()
    {
        if (_hostInfoList is null)
        {
            _hostInfoList = new();
        }
		return _hostInfoList;
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
			OnPackageChanged?.Invoke(availablePackage);
            _packageList = null;
		}
	}


    public void AddOrUpdateHost(Models.HostInfo hostInfo)
    {
        var list = GetHostList();
        var existing = list.SingleOrDefault(i => i.HostName == hostInfo.HostName);
        if (existing == null)
        {
            list.Add(hostInfo);
        }
        else
        {
            existing.LastHitDate = DateTime.Now;
            existing.ExternalIp = hostInfo.ExternalIp;
            existing.Version = hostInfo.Version;
            existing.CreationDate = hostInfo.CreationDate;
            existing.HostState = hostInfo.HostState;
        }
        OnHostChanged?.Invoke(hostInfo);
    }

    public void AddOrUpdateMicroServiceInfo(Models.ExtendedMicroServiceInfo microserviceInfo)
    {
        var serviceList = GetServiceList();

        var rms = serviceList.SingleOrDefault(i => i.Key == microserviceInfo.Key);
        if (rms == null)
        {
            rms = new Models.ExtendedMicroServiceInfo();
            rms.HostName = microserviceInfo.HostName;
            rms.ServiceName = microserviceInfo.ServiceName;
            serviceList.Add(rms);
        }
        rms.Location = microserviceInfo.Location;
        rms.UserInteractive = microserviceInfo.UserInteractive;
        rms.Version = microserviceInfo.Version;
        rms.LastWriteTime = microserviceInfo.LastWriteTime;
        rms.ThreadCount = microserviceInfo.ThreadCount;
        rms.ProcessId = microserviceInfo.ProcessId;
        rms.ServiceState = microserviceInfo.ServiceState;
        rms.StartedDate = microserviceInfo.StartedDate;
        rms.CommandLine = microserviceInfo.CommandLine;
        rms.PeakPagedMem = microserviceInfo.PeakPagedMem;
        rms.PeakVirtualMem = microserviceInfo.PeakVirtualMem;
        rms.PeakWorkingSet = microserviceInfo.PeakWorkingSet;
        rms.WorkingSet = microserviceInfo.WorkingSet;
        rms.EnvironmentName = microserviceInfo.EnvironmentName;
        rms.LastHitDate = microserviceInfo.LastHitDate;
        rms.Log = microserviceInfo.Log;

        OnServiceChanged?.Invoke(rms);
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
                _packageList = null;
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
            _packageList = null;
            OnPackageChanged?.Invoke(package);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return null;
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
		
        var serviceList = GetServiceList();
		var rms = serviceList.SingleOrDefault(i => i.Key == rmi.Key);
		if (rms != null)
		{
			serviceList.Remove(rms);
			OnServiceChanged?.Invoke(rms);
		}
	}
}
