namespace Palace.Server.Services;

public class DefaultPackageDownloaderService : IPackageDownloaderService
{
    private readonly Configuration.GlobalSettings _settings;
    private readonly Dictionary<Guid, string> _urls = new();

    public DefaultPackageDownloaderService(Configuration.GlobalSettings settings)
    {
        _settings = settings;
    }

    public async Task<string> GenerateUrl(string packageFileName)
    {
        await Task.Yield();
        var tempId = Guid.NewGuid();
        var result = $"{_settings.CurrentUrl}/api/palace/download/{tempId}/{packageFileName}";
        _urls.Add(tempId, packageFileName);
        return result;
    }

    public bool IsKeyExists(Guid key)
    {
        return _urls.ContainsKey(key);
    }
}