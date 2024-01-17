using ArianeBus;

namespace Palace.Server.Services;
public class AlwaysStartedCheckerService : BackgroundService
{
	private readonly Orchestrator _orchestrator;
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly IServiceBus _bus;
	private readonly Configuration.GlobalSettings _settings;

	public AlwaysStartedCheckerService(Orchestrator orchestrator,
		IServiceScopeFactory serviceScopeFactory,
		IServiceBus bus,
		Configuration.GlobalSettings settings)
	{
		_orchestrator = orchestrator;
		_serviceScopeFactory = serviceScopeFactory;
		_bus = bus;
		_settings = settings;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var settingsRepository = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ServiceSettingsRepository>();
			var serviceSettings = await settingsRepository.GetAll();
			var hostList = _orchestrator.GetHostList();
			foreach (var item in serviceSettings)
			{
				foreach (var host in hostList)
				{
					if (host.HostState != HostState.Running)
					{
						continue;
					}
					var key = $"{host.HostName}__{item.ServiceName}".ToLower();
					var runningService = _orchestrator.GetExtendedMicroServiceInfoByKey(key);
					if (runningService is null
						|| runningService.ServiceState == ServiceState.Offline)
					{
						var arguments = await settingsRepository.GetArgumentsByHostForService(host.HostName, item.Id);
						var publishTopic = _bus.PublishTopic(_settings.StartServiceTopicName, new Shared.Messages.StartService
						{
							ActionId = Guid.NewGuid(),
							HostName = host.HostName,
							ServiceSettings = item,
							OverridedArguments = arguments is null ? null : arguments.Arguments
						});
					}
				}
			}
			await Task.Delay(30 * 1000);
		}
	}
}
