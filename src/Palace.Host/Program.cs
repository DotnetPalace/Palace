#define WINDOWS
using ArianeBus;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using LogRPush;

using Palace.Host;
using Palace.Host.Extensions;

IHost host = Host.CreateDefaultBuilder(args)
#if WINDOWS
    .UseWindowsService()
#endif
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        var currentDirectory = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
        config
            .SetBasePath(currentDirectory)
            .AddJsonFile("appSettings.json")
            .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.local.json", optional: true, reloadOnChange: false);

        hostingContext.HostingEnvironment.ApplicationName = "Palace.Host";
    })
    .ConfigureServices((hostingContext, services) =>
    {
        var section = hostingContext.Configuration.GetSection("Palace");
        var settings = new Palace.Host.Configuration.GlobalSettings();
        section.Bind(settings);
        settings.Initialize(); 
        services.AddSingleton(settings); 

        services.AddMemoryCache();
        services.AddLogging(configure =>
        {
            configure.AddConsole();
        });

        var vaultUri = new Uri($"https://{settings.KeyVaultName}.vault.azure.net");
        var credential = new ClientSecretCredential(settings.KeyVaultTenantId, settings.KeyVaultClientId, settings.KeyVaultClientSecret);
        var client = new SecretClient(vaultUri, credential);

        var apiKeySecret = client.GetSecretAsync("ApiKey").Result;
        settings.SetApiKey(new Guid(apiKeySecret.Value.Value));

        var azureBusConnectionStringSecret = client.GetSecretAsync("AzureBusConnectionString").Result;
        settings.SetAzureBusConnectionString(azureBusConnectionStringSecret.Value.Value);

		services.AddHostedService<MainWorker>();

		services.AddArianeBus(config =>
        {
            config.PrefixName = settings.QueuePrefix;
            config.BusConnectionString = settings.AzureBusConnectionString;
            config.RegisterTopicReader<Palace.Host.MessageReaders.InstallService>(new TopicName(settings.InstallServiceTopicName), new SubscriptionName(settings.HostName));
            config.RegisterTopicReader<Palace.Host.MessageReaders.StartService>(new TopicName(settings.StartServiceTopicName), new SubscriptionName(settings.HostName));
            config.RegisterTopicReader<Palace.Host.MessageReaders.UninstallService>(new TopicName(settings.UnInstallServiceTopicName), new SubscriptionName(settings.HostName));
			config.RegisterTopicReader<Palace.Host.MessageReaders.ServerReset>(new TopicName(settings.ServerResetTopicName), new SubscriptionName(settings.HostName));
		});

        var version = $"{typeof(Program).Assembly.GetName().Version}";
        services.AddHttpClient("PalaceServer", configure =>
        {
            configure.DefaultRequestHeaders.Add("Authorization", $"Basic {settings.ApiKey}");
            configure.DefaultRequestHeaders.UserAgent.ParseAdd($"Palace/{version} ({System.Environment.OSVersion}; {System.Environment.MachineName}; {settings.HostName})");
        });

        if (settings.LogServerUrl is not null)
        {
            services.AddLogRPush(config =>
            {
                config.HostName = "PalaceHost";
                config.LogServerUrlList.Add(settings.LogServerUrl);
                config.EnvironmentName = hostingContext.HostingEnvironment.EnvironmentName;
            });
        }
    })
    .Build();

var settings = host.Services.GetRequiredService<Palace.Host.Configuration.GlobalSettings>();
if (settings.LogServerUrl is not null)
{
    host.Services.UseLogRPush();
}

await host.RunAsync();
