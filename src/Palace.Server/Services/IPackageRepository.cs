namespace Palace.Server.Services;

public interface IPackageRepository
{
    event Action<PackageInfo> PackageChanged;

    void BackupAndUpdateRepositoryFile(string zipFileFullPath);
    List<FileInfo> GetBackupFileList(string packageFileName);
    IEnumerable<PackageInfo> GetPackageInfoList();
    Task<string?> RemovePackage(string packageFileName);
    string? RollbackPackage(PackageInfo package, FileInfo fileInfo);
}