namespace Palace.Server.Services;

public class DefaultPackageDownloaderService(Configuration.GlobalSettings settings) : IPackageDownloaderService
{
    private readonly Dictionary<Guid, string> _urls = new();

    public async Task<string> GenerateUrl(string packageFileName)
    {
        await Task.Yield();
        var tempId = Guid.NewGuid();
        var result = $"{settings.CurrentUrl}/api/palace/download/{tempId}/{packageFileName}";
        _urls.Add(tempId, packageFileName);
        return result;
    }

    public bool IsKeyExists(Guid key)
    {
        return _urls.ContainsKey(key);
    }
}