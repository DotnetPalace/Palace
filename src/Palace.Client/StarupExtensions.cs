using ArianeBus;

using Microsoft.Extensions.DependencyInjection;

namespace Palace.Client;

public static class StarupExtensions
{
    public static IServiceCollection AddPalaceClient(this IServiceCollection services, Action<PalaceSettings> action)
    {
        var settings = new PalaceSettings();
        action(settings);
        services.AddSingleton(settings);

        if (string.IsNullOrEmpty(settings.ServiceName))
        {
            throw new ArgumentNullException(nameof(settings.ServiceName));
        }

        if (string.IsNullOrEmpty(settings.HostEnvironmentName))
		{
			throw new ArgumentNullException(nameof(settings.HostEnvironmentName));
		}

        services.AddArianeBus(config =>
        {
            config.PrefixName = settings.QueuePrefix;
            config.BusConnectionString = settings.AzureBusConnectionString;
            config.RegisterTopicReader<StopMessageReader>(new TopicName(settings.StopTopicName), new SubscriptionName(settings.Key));
        });

        services.AddHostedService<MainWorker>();

        return services;
    }
}
