using System.Collections.Concurrent;

using ArianeBus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

using Palace.Server.Models;
using Palace.Server.Services.UpdateHandler;

namespace Palace.Server.Services;

public class UpdaterService : BackgroundService
{
	private readonly Orchestrator _orchestrator;
	private readonly IDbContextFactory<PalaceDbContext> _dbContextFactory;
	private readonly Configuration.GlobalSettings _settings;
	private readonly ILogger<UpdaterService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ConcurrentDictionary<string, Models.MicroserviceUpdateContext> _processList = new();

	public UpdaterService(Orchestrator orchestrator,
		IDbContextFactory<PalaceDbContext> dbContextFactory,
		Configuration.GlobalSettings settings,
		ILogger<UpdaterService> logger,
		IServiceScopeFactory serviceScopeFactory)
	{
		_orchestrator = orchestrator;
		_dbContextFactory = dbContextFactory;
		_settings = settings;
		_logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _orchestrator.OnPackageChanged += OnPackageChanged;
	}

	public override Task StartAsync(CancellationToken cancellationToken)
	{
		return base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var toStartList = from p in _processList
								where p.Value.CurrentWorkflow == "Start"
								select p;

			foreach (var item in toStartList)
			{
				var otherHosts = from p in _processList
								 where p.Value.ServiceInfo.ServiceName == item.Value.ServiceInfo.ServiceName
									&& p.Value.HostName != item.Value.HostName
									&& p.Value.CurrentWorkflow != "Start"
								 select p;

				// One update by host for the same service
				if (!otherHosts.Any())
				{
					item.Value.CurrentWorkflow = "Starting";
					RunUpdateTask(item.Value, stoppingToken);
					await Task.Delay(2 * 1000);
				}
			}
			await Task.Delay(1 * 1000, stoppingToken);
		}
	}

	public override Task StopAsync(CancellationToken cancellationToken)
	{
		return base.StopAsync(cancellationToken);
	}

    private void RunUpdateTask(Models.MicroserviceUpdateContext context, CancellationToken cancellationToken)
    {
        Task.Run(() => ProcessUpdate(context, cancellationToken));
    }

    private async Task ProcessUpdate(MicroserviceUpdateContext context, CancellationToken cancellationToken)
	{
        _logger.LogInformation("Update service detected {serviceName} for host {host}", context.ServiceInfo.ServiceName, context.HostName);

        context.CurrentWorkflow = "Working";

		using var scope = _serviceScopeFactory.CreateScope();
		var handlers = scope.ServiceProvider.GetServices<IUpdateHandler>();

		var handler = handlers.Single(i => i.Name == nameof(SaveServiceStateHandler));
		handler.AddNextHandler(handlers.Single(i => i.Name == nameof(StopServiceHandler)));
		handler.AddNextHandler(handlers.Single(i => i.Name == nameof(InstallServiceHandler)));
		handler.AddNextHandler(handlers.Single(i => i.Name == nameof(StartServiceHandler)));

		await handler.ProcessUpdateAsync(context, cancellationToken);

		_processList.TryRemove(context.Key, out _);
		_orchestrator.AddOrUpdateMicroServiceInfo(context.ServiceInfo);
		context.Dispose();
    }

	private async void OnPackageChanged(PackageInfo package)
	{
		var db = await _dbContextFactory.CreateDbContextAsync();

		// Recherche dans tous les settings si le package est présent
		var query = from mss in db.MicroServiceSettings
					where mss.PackageFileName == package.PackageFileName
					select mss;

		var settingsList = await query.ToListAsync();
		if (settingsList.Count == 0)
		{
			// Pas de service à mettre à jour correspondant à ce package
			return;
		}

		var serviceNameList = settingsList.Select(i => i.ServiceName).Distinct().ToList();

		var services = _orchestrator.GetServiceList()
						.Where(i => serviceNameList.Contains(i.ServiceName))
						.ToList();

		var hostList = services.Select(i => i.HostName).Distinct().ToList();

		// Boucler sur la liste des hosts qui ont ce package et leur demander de se mettre à jour
		foreach (var host in hostList)
		{
			foreach (var service in services)
			{
				var settings = settingsList.Single(i => i.ServiceName == service.ServiceName);
				var muc = new MicroserviceUpdateContext
				{
					HostName = host,
					ServiceSettings = settings,
					ServiceInfo = service,
					CurrentWorkflow = "Start"
				};
				if (!_processList.ContainsKey(muc.Key))
				{
                    _processList.TryAdd(muc.Key, muc);
                }
            }
		}
	}

}
