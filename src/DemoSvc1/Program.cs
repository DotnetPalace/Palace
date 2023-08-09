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

		var keyVaultName = section.GetValue<string>("KeyVaultName");
		var keyVaultTenantId = section.GetValue<string>("KeyVaultTenantId");
		var KeyVaultClientId = section.GetValue<string>("KeyVaultClientId");
		var KeyVaultClientSecret = section.GetValue<string>("KeyVaultClientSecret");

		var vaultUri = new Uri($"https://{keyVaultName}.vault.azure.net");
		var credential = new ClientSecretCredential(keyVaultTenantId, KeyVaultClientId, KeyVaultClientSecret);
		var client = new SecretClient(vaultUri, credential);

		var apiKeySecret = client.GetSecretAsync("ApiKey").Result;
		var apiKey = new Guid(apiKeySecret.Value.Value);

		var azureBusConnectionStringSecret = client.GetSecretAsync("AzureBusConnectionString").Result;
		var azureBusConnectionString = azureBusConnectionStringSecret.Value.Value;

		services.AddPalaceClient(config =>
		{
			config.ServiceName = nameof(DemoSvc1);
			config.AzureBusConnectionString = azureBusConnectionString;
			config.StopServiceReportQueueName = "palace.stopservicereport";
			config.ServiceHealthQueueName = "palace.servicehealth";
			config.StopTopicName = "palace.stopservice";
			config.HostEnvironmentName = hostingContext.HostingEnvironment.EnvironmentName;
		});

		services.AddHostedService<PingWorker>();
	})
	.Build();

await host.RunAsync();
