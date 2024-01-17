using Palace.Server.Models;
using Palace.Server.Services.UpdateStrategies;

namespace Palace.Server.Services;

public class UpdaterService : BackgroundService
{
	private readonly Orchestrator _orchestrator;
	private readonly Configuration.GlobalSettings _settings;
	private readonly ILogger<UpdaterService> _logger;
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly IPackageRepository _packageRepository;
	private readonly ConcurrentDictionary<string, Models.MicroserviceUpdateContext> _processList = new(comparer: StringComparer.InvariantCultureIgnoreCase);
	private readonly UpdateStrategyBase _updateStrategyBase = default!;

	public UpdaterService(Orchestrator orchestrator,
		Configuration.GlobalSettings settings,
		ILogger<UpdaterService> logger,
		IEnumerable<UpdateStrategyBase> updateStrategies,
		IServiceScopeFactory serviceScopeFactory,
		IPackageRepository packageRepository)
	{
		_orchestrator = orchestrator;
		_settings = settings;
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
		_packageRepository = packageRepository;
		_packageRepository.PackageChanged += OnPackageChanged;

		_updateStrategyBase = updateStrategies.Single(i => i.Name.Equals(_settings.DefaultUpdateStrategyName, StringComparison.InvariantCultureIgnoreCase));
		_updateStrategyBase.Initialize(_processList);
	}

	public override Task StartAsync(CancellationToken cancellationToken)
	{
		return base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			_updateStrategyBase.ProcessNextUpdate(stoppingToken);
			await Task.Delay(1 * 1000, stoppingToken);
		}
	}

	public override Task StopAsync(CancellationToken cancellationToken)
	{
		if (_packageRepository is not null)
		{
			_packageRepository.PackageChanged -= OnPackageChanged;
		}
		return base.StopAsync(cancellationToken);
	}

	private async void OnPackageChanged(PackageInfo package)
	{
		using var scope = _serviceScopeFactory.CreateScope();
		var serviceSettingRepository = scope.ServiceProvider.GetRequiredService<ServiceSettingsRepository>();

		var settingsList = await serviceSettingRepository.GetListByPackageFileName(package.PackageFileName);

		if (settingsList.Count() == 0)
		{
			// Pas de service à mettre à jour correspondant à ce package
			return;
		}

		var serviceNameList = settingsList.Select(i => i.ServiceName).Distinct().ToList();

		var services = _orchestrator.GetServiceList()
						.Where(i => serviceNameList.Contains(i.ServiceName))
						.ToList();

		var hostNameList = services.Select(i => i.HostName).Distinct().ToList();

		// Boucler sur la liste des hosts qui ont ce package
		// et faire se mettre à jour
		foreach (var serviceName in serviceNameList)
		{
			foreach (var host in hostNameList)
			{
				var key = $"{host}__{serviceName}".ToLower();
				var service = services.SingleOrDefault(i => i.Key == key);
				if (service is null)
				{
					continue;
				}

				var serviceSettings = settingsList.Single(i => i.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));
				var arguments = await serviceSettingRepository.GetArgumentsByHostForService(host, serviceSettings.Id);
				var muc = new MicroserviceUpdateContext
				{
					HostName = host,
					ServiceSettings = serviceSettings,
					ServiceInfo = service,
					CurrentStep = "Pending",
					Origin = "Update",
					OverridedArguments = arguments?.Arguments
				};
				if (!_processList.ContainsKey(muc.Key))
				{
					_processList.TryAdd(muc.Key, muc);
				}
			}
		}
	}

}
