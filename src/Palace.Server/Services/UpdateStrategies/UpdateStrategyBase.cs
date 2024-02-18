using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;
using Palace.Server.Models;
using Palace.Server.Services.UpdateHandler;

namespace Palace.Server.Services.UpdateStrategies;

public abstract class UpdateStrategyBase
{
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly ILogger _logger;
	private readonly Orchestrator _orchestrator;
	protected ConcurrentDictionary<string, Models.MicroserviceUpdateContext> _contextList = default!;

	protected UpdateStrategyBase(IServiceScopeFactory serviceScopeFactory,
		ILogger<UpdateStrategyBase> logger,
		Orchestrator orchestrator)
    {
		_serviceScopeFactory = serviceScopeFactory;
		_logger = logger;
		_orchestrator = orchestrator;
	}

	public abstract string Name { get; }

	public virtual void Initialize(ConcurrentDictionary<string, Models.MicroserviceUpdateContext> list)
	{
		_contextList = list;
	}

	public abstract void ProcessNextUpdate(CancellationToken cancellationToken);

	protected async Task ProcessUpdate(MicroserviceUpdateContext context, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Update service detected {serviceName} for host {host}", context.ServiceInfo.ServiceName, context.HostName);

		context.CurrentStep = "Processing";

		using var scope = _serviceScopeFactory.CreateScope();
		var handlers = scope.ServiceProvider.GetServices<IUpdateHandler>();

		var handler = handlers.Single(i => i.Name == nameof(SaveServiceStateHandler));
		handler.AddNextHandler(handlers.Single(i => i.Name == nameof(StopServiceHandler)));
		handler.AddNextHandler(handlers.Single(i => i.Name == nameof(InstallServiceHandler)));
		handler.AddNextHandler(handlers.Single(i => i.Name == nameof(StartServiceHandler)));

		await handler.ProcessUpdateAsync(context, cancellationToken);

		if (!_contextList.TryRemove(context.Key, out _))
		{
			_logger.LogWarning("ProcessUpdate - Could not remove process {process} from list", context.Key);
		}
		_orchestrator.AddOrUpdateMicroServiceInfo(context.ServiceInfo);
		context.Dispose();
	}

}
