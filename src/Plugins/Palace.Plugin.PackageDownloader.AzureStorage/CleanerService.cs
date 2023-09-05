using Microsoft.Extensions.Hosting;

namespace Palace.Plugin.PackageDownloader.AzureStorage;

internal class CleanerService : BackgroundService
{
    private readonly AzureStorageService _azureStorageService;

    public CleanerService(AzureStorageService azureStorageService)
    {
        _azureStorageService = azureStorageService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // On recherche tous les fichiers dans Azure Storage qui ont plus d'une heure pour les supprimer
            await _azureStorageService.DeleteExpiredPackageFiles(stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
