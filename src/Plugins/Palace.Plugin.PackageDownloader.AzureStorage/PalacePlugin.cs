using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Palace.Server.Services;

namespace Palace.Plugin.PackageDownloader.AzureStorage;

public class PalacePlugin : IPalacePlugin
{
	private AzureStorageConfiguration _settings = new();
	public string Name => "AzureStorage";

	public Task Configure(IServiceCollection services, IConfiguration configuration)
	{
		try
		{
			var section = configuration.GetRequiredSection("Palace.AzureStorage");
			section.Bind(_settings);
		}
		catch (Exception)
		{
			Console.WriteLine("Section Palace.AzureStorage not found in configuration");
			return Task.CompletedTask;
		}

		services.AddSingleton(_settings);

		services.AddSingleton<IPackageDownloaderService, PackageDownloader>();
		services.AddSingleton<AzureStorageService>();
		services.AddHostedService<CleanerService>();

		return Task.CompletedTask;
	}
}