namespace Palace.Shared;

public interface IPackageDownloaderService
{
    Task<string> GenerateUrl(string packageFileName);
    bool IsKeyExists(Guid key);
}
