using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Palace.Shared;

namespace Palace.Host.Extensions;

public static class GlobalExtensions
{
    public static void InitializeFolders(this Configuration.GlobalSettings palaceSettings)
    {
        var currentDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
        if (palaceSettings.UpdateFolder.StartsWith(@".\"))
        {
            palaceSettings.UpdateFolder = Path.Combine(currentDirectory, palaceSettings.UpdateFolder.Replace(@".\", string.Empty));
        }
        if (palaceSettings.DownloadFolder.StartsWith(@".\"))
        {
            palaceSettings.DownloadFolder = Path.Combine(currentDirectory, palaceSettings.DownloadFolder.Replace(@".\", string.Empty));
        }
        if (palaceSettings.InstallationFolder.StartsWith(@".\"))
        {
            palaceSettings.InstallationFolder = Path.Combine(currentDirectory, palaceSettings.InstallationFolder.Replace(@".\", string.Empty));
        }

        if (!Directory.Exists(palaceSettings.UpdateFolder))
		{
			Directory.CreateDirectory(palaceSettings.UpdateFolder);
		}
        if (!Directory.Exists(palaceSettings.DownloadFolder))
        {
            Directory.CreateDirectory(palaceSettings.DownloadFolder);
        }
        if (!Directory.Exists(palaceSettings.InstallationFolder))
		{
			Directory.CreateDirectory(palaceSettings.InstallationFolder);
		}
    }

    public static async Task SetParmetersFromSecrets(this Configuration.GlobalSettings settings, IServiceCollection services, IConfiguration configuration)
    {
		var currentFolder = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
		var secretAssemblies = System.IO.Directory.GetFiles(currentFolder, "Palace.Secret.*.dll");
		foreach (var secretAssemblyFile in secretAssemblies)
		{
			Assembly.LoadFrom(secretAssemblyFile);
		}
		var palaceAssemblies = AppDomain.CurrentDomain
								.GetAssemblies()
								.Where(a => a.FullName!.IndexOf("Palace.Secret.") != -1);

		var secretTypeList = palaceAssemblies
								.SelectMany(i => i.GetTypes()
								.Where(i => !i.IsInterface && typeof(ISecretValueReader).IsAssignableFrom(i)));

		foreach (var secretType in secretTypeList)
		{
			var secretReader = (ISecretValueReader)Activator.CreateInstance(secretType)!;
			if (secretReader.Name.Equals(settings.SecretConfigurationReaderName, StringComparison.InvariantCultureIgnoreCase))
			{
				secretReader.Configure(services, configuration);

				settings.ApiKey = new Guid(await secretReader.GetSecretValue("ApiKey"));
				settings.AzureBusConnectionString = await secretReader.GetSecretValue("AzureBusConnectionString");

				break;
			}
		}
	}
}
