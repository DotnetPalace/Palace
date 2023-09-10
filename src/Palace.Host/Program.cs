#define WINDOWS
using System.Reflection;

using ArianeBus;

using LogRPush;

using Palace.Host;
using Palace.Host.Extensions;
using Palace.Shared;

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
        settings.InitializeFolders(); 
        services.AddSingleton(settings);

        if (!string.IsNullOrWhiteSpace(settings.SecretConfigurationReaderName)
             && !settings.SecretConfigurationReaderName.Equals("NoSecret", StringComparison.InvariantCultureIgnoreCase))
        {
            settings.SetParametersFromSecrets(services, hostingContext.Configuration).Wait();
		}

		services.AddHostedService<MainWorker>();
		services.AddMemoryCache();
		services.AddLogging();

		services.AddArianeBus(config =>
        {
            config.PrefixName = settings.QueuePrefix;
            config.BusConnectionString = settings.AzureBusConnectionString;
            config.RegisterTopicReader<Palace.Host.MessageReaders.InstallService>(new TopicName(settings.InstallServiceTopicName), new SubscriptionName(settings.HostName));
            config.RegisterTopicReader<Palace.Host.MessageReaders.StartService>(new TopicName(settings.StartServiceTopicName), new SubscriptionName(settings.HostName));
            config.RegisterTopicReader<Palace.Host.MessageReaders.UninstallService>(new TopicName(settings.UnInstallServiceTopicName), new SubscriptionName(settings.HostName));
			config.RegisterTopicReader<Palace.Host.MessageReaders.ServerReset>(new TopicName(settings.ServerResetTopicName), new SubscriptionName(settings.HostName));
			config.RegisterTopicReader<Palace.Host.MessageReaders.KillService>(new TopicName(settings.KillServiceTopicName), new SubscriptionName(settings.HostName));
		});

        var version = $"{typeof(Program).Assembly.GetName().Version}";
        services.AddHttpClient("PalaceServer", configure =>
        {
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
