using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Palace.PackageDownloader.AzureStorage.Plugin;
using Palace.Shared;

namespace Palace.PackageDownloader.AzureStorage;

public class PalacePlugin : IPalacePlugin
{
    private AzureStorageConfiguration _settings = new();
    public string Name => "AzureStorage";

    public Task Configure(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Palace.AzureStorage");
        if (section.Value is null)
        {
            Console.WriteLine("Section Palace.AzureStorage not found in configuration");
            return Task.CompletedTask;
        }
        section.Bind(_settings);

        services.AddSingleton(_settings);

        services.AddSingleton<IPackageDownloaderService, PackageDownloader>();
        services.AddSingleton<AzureStorageService>();
        services.AddHostedService<CleanerService>();

        return Task.CompletedTask;
    }
}