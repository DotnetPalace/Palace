using ArianeBus;

namespace Palace.Server.Services;
public class AlwaysStartedCheckerService(
	Orchestrator orchestrator,
    IServiceScopeFactory serviceScopeFactory,
    IServiceBus bus,
    Configuration.GlobalSettings settings
	) 
	: BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var settingsRepository = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ServiceSettingsRepository>();
			var serviceSettings = await settingsRepository.GetAll();
			var hostList = orchestrator.GetHostList();
			foreach (var item in serviceSettings)
			{
				foreach (var host in hostList)
				{
					if (host.HostState != HostState.Running)
					{
						continue;
					}
					var key = $"{host.HostName}__{item.ServiceName}".ToLower();
					var runningService = orchestrator.GetExtendedMicroServiceInfoByKey(key);
					if (runningService is null
						|| runningService.ServiceState == ServiceState.Offline)
					{
						var arguments = await settingsRepository.GetArgumentsByHostForService(host.HostName, item.Id);
						await bus.PublishTopic(settings.StartServiceTopicName, new Shared.Messages.StartService
						{
							ActionId = Guid.NewGuid(),
							HostName = host.HostName,
							ServiceSettings = item,
							OverridedArguments = arguments is null ? null : arguments.Arguments
						}, cancellationToken: stoppingToken);
					}
				}
			}
			await Task.Delay(30 * 1000);
		}
	}
}
