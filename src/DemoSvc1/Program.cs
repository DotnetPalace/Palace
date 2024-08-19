// See https://aka.ms/new-console-template for more information
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using DemoSvc1;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Palace.Client;

Console.WriteLine("Hello, Palace!");

IHost host = Host.CreateDefaultBuilder(args)
	.ConfigureAppConfiguration((hostingContext, config) =>
	{
		var currentDirectory = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location)!;

		config
			.SetBasePath(currentDirectory)
			.AddJsonFile("appSettings.json")
			.AddJsonFile($"appsettings.local.json", optional: true, reloadOnChange: false)
			.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false);

		hostingContext.HostingEnvironment.ApplicationName = "Palace.Host";
	})
	.ConfigureServices((hostingContext, services) =>
	{
		var section = hostingContext.Configuration.GetSection("Palace");

		var prefixQueue = section.GetValue<string>("QueuePrefix");

        var keyVaultName = hostingContext.Configuration.GetValue<string>("Palace.KeyVaultProvider:KeyVaultName")!;
        var keyVaultTenantId = hostingContext.Configuration.GetValue<string>("Palace.KeyVaultProvider:KeyVaultTenantId")!;
        var keyVaultClientId = hostingContext.Configuration.GetValue<string>("Palace.KeyVaultProvider:KeyVaultClientId")!;
        var keyVaultClientSecret = hostingContext.Configuration.GetValue<string>("Palace.KeyVaultProvider:KeyVaultClientSecret")!;

        var vaultUri = new Uri($"https://{keyVaultName}.vault.azure.net");
		var credential = new ClientSecretCredential(keyVaultTenantId, keyVaultClientId, keyVaultClientSecret);
		var client = new SecretClient(vaultUri, credential);

		var apiKeySecret = client.GetSecretAsync("ApiKey").Result;
		var apiKey = new Guid(apiKeySecret.Value.Value);

		var azureBusConnectionStringSecret = client.GetSecretAsync("AzureBusConnectionString").Result;
		var azureBusConnectionString = azureBusConnectionStringSecret.Value.Value;

		var hostName = args.GetParameterValue("--hostname");
		var serviceName = args.GetParameterValue("--servicename");
		if (string.IsNullOrWhiteSpace(serviceName))
		{
			serviceName = nameof(DemoSvc1);
		}

		services.AddPalaceClient(config =>
		{
			config.HostName = hostName ?? section.GetValue<string>("HostName") ?? config.HostName;
			config.ServiceName = serviceName ?? nameof(DemoSvc1);
			config.QueuePrefix = prefixQueue;
			config.AzureBusConnectionString = azureBusConnectionString;
			config.HostEnvironmentName = hostingContext.HostingEnvironment.EnvironmentName;
		});

		services.AddHostedService<PingWorker>();
	})
	.Build();

await host.RunAsync();
