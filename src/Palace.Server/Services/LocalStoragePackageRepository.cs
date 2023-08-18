using System.Collections.Concurrent;
using System.Runtime;
using Palace.Server.Pages;

namespace Palace.Server.Services;

public class LocalStoragePackageRepository : IPackageRepository
{
    private readonly ILogger<LocalStoragePackageRepository> _logger;
    private readonly Configuration.GlobalSettings _settings;
    private readonly ConcurrentDictionary<string, PackageInfo> _packageList = new(comparer: StringComparer.InvariantCultureIgnoreCase);

    public event Action<PackageInfo> PackageChanged = default!;

    public LocalStoragePackageRepository(ILogger<LocalStoragePackageRepository> logger,
        Configuration.GlobalSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public IEnumerable<PackageInfo> GetPackageInfoList()
    {
        if (_packageList.Count == 0)
        {
            LoadPackageList();
        }
        return _packageList.Select(i => i.Value);
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
            PackageChanged?.Invoke(availablePackage);
        }
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

    public string? RollbackPackage(PackageInfo package, FileInfo fileInfo)
    {
        var destPackage = Path.Combine(_settings.RepositoryFolder, package.PackageFileName);
        try
        {
            fileInfo.LastWriteTime = DateTime.Now;
            fileInfo.CreationTime = DateTime.Now;
            File.Copy(fileInfo.FullName, destPackage, true);
            LoadPackageList();
            PackageChanged?.Invoke(package);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return null;
    }

    private void LoadPackageList()
    {
        _packageList.Clear();
        var zipFileList = from f in Directory.GetFiles(_settings.RepositoryFolder, "*.zip", SearchOption.AllDirectories)
                          let fileInfo = new FileInfo(f)
                          select fileInfo;

        foreach (var item in zipFileList)
        {
            var info = new Palace.Shared.PackageInfo
            {
                PackageFileName = item.Name,
                Location = item.FullName,
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

    private void SetCurrentVersion(PackageInfo availablePackage)
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

}
