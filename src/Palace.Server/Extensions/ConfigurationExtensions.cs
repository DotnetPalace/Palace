using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;

using Palace.Server.Services;

namespace Palace.Server.Extensions;

public static class ConfigurationExtensions
{
    public static void PrepareFolders(this Configuration.GlobalSettings settings)
    {
        var directoryName = System.IO.Path.GetDirectoryName(typeof(ConfigurationExtensions).Assembly.Location)!;
        if (settings.RepositoryFolder.StartsWith(@".\"))
        {
            settings.RepositoryFolder = System.IO.Path.Combine(directoryName, settings.RepositoryFolder.Replace(@".\", ""));
        }
        if (settings.StagingFolder.StartsWith(@".\"))
        {
            settings.StagingFolder = System.IO.Path.Combine(directoryName, settings.StagingFolder.Replace(@".\", ""));
        }
        if (settings.BackupFolder.StartsWith(@".\"))
        {
            settings.BackupFolder = System.IO.Path.Combine(directoryName, settings.BackupFolder.Replace(@".\", ""));
        }
        if (settings.TempFolder.StartsWith(@".\"))
        {
            settings.TempFolder = System.IO.Path.Combine(directoryName, settings.TempFolder.Replace(@".\", ""));
        }
		if (settings.DataFolder.StartsWith(@".\"))
		{
			settings.DataFolder = System.IO.Path.Combine(directoryName, settings.DataFolder.Replace(@".\", ""));
		}

		if (!System.IO.Directory.Exists(settings.RepositoryFolder))
        {
            System.IO.Directory.CreateDirectory(settings.RepositoryFolder);
        }
        if (!System.IO.Directory.Exists(settings.StagingFolder))
        {
            System.IO.Directory.CreateDirectory(settings.StagingFolder);
        }
        if (!System.IO.Directory.Exists(settings.BackupFolder))
        {
            System.IO.Directory.CreateDirectory(settings.BackupFolder);
        }
        if (!System.IO.Directory.Exists(settings.TempFolder))
        {
            System.IO.Directory.CreateDirectory(settings.TempFolder);
        }
		if (!System.IO.Directory.Exists(settings.DataFolder))
		{
			System.IO.Directory.CreateDirectory(settings.DataFolder);
		}
	}

	public static async Task SetParametersFromSecrets(this Configuration.GlobalSettings settings, WebApplicationBuilder builder)
	{
        var currentFolder = System.IO.Path.GetDirectoryName(typeof(ConfigurationExtensions).Assembly.Location)!;
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
				secretReader.Configure(builder.Services, builder.Configuration);

				settings.ApiKey = new Guid(await secretReader.GetSecretValue("ApiKey"));
				settings.AzureBusConnectionString = await secretReader.GetSecretValue("AzureBusConnectionString");
                settings.AdminKey = await secretReader.GetSecretValue("AdminKey");

				break;
			}
		}
	}

}
