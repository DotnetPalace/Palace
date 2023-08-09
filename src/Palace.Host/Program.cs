#define WINDOWS
using ArianeBus;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

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
            .AddJsonFile($"appsettings.local.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false);

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

        services.AddHostedService<MainWorker>();

        var vaultUri = new Uri($"https://{settings.KeyVaultName}.vault.azure.net");
        var credential = new ClientSecretCredential(settings.KeyVaultTenantId, settings.KeyVaultClientId, settings.KeyVaultClientSecret);
        var client = new SecretClient(vaultUri, credential);

        var apiKeySecret = client.GetSecretAsync("ApiKey").Result;
        settings.SetApiKey(new Guid(apiKeySecret.Value.Value));

        var azureBusConnectionStringSecret = client.GetSecretAsync("AzureBusConnectionString").Result;
        settings.SetAzureBusConnectionString(azureBusConnectionStringSecret.Value.Value);

        services.AddArianeBus(config =>
        {
            config.BusConnectionString = settings.AzureBusConnectionString;
            config.RegisterTopicReader<Palace.Host.MessageReaders.InstallService>(new TopicName(settings.InstallServiceTopicName), new SubscriptionName(settings.HostName));
            config.RegisterTopicReader<Palace.Host.MessageReaders.StartService>(new TopicName(settings.StartServiceTopicName), new SubscriptionName(settings.HostName));
        });

        var version = $"{typeof(Program).Assembly.GetName().Version}";
        services.AddHttpClient("PalaceServer", configure =>
        {
            configure.DefaultRequestHeaders.Add("Authorization", $"Basic {settings.ApiKey}");
            configure.DefaultRequestHeaders.UserAgent.ParseAdd($"Palace/{version} ({System.Environment.OSVersion}; {System.Environment.MachineName}; {settings.HostName})");
        });
    })
    .Build();

await host.RunAsync();
