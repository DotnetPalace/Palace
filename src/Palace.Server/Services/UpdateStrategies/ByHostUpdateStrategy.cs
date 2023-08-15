using System.Collections.Concurrent;

using Palace.Server.Models;

namespace Palace.Server.Services.UpdateStrategies;

public class ByHostUpdateStrategy : UpdateStrategyBase
{
	public ByHostUpdateStrategy(IServiceScopeFactory serviceScopeFactory,
		ILogger<ByHostUpdateStrategy> logger,
		Orchestrator orchestrator)
		: base(serviceScopeFactory, logger, orchestrator)
    {
            
    }

	public override string Name => "ByHost";

	public override void ProcessNextUpdate(CancellationToken cancellationToken)
	{
		var pending = _contextList.FirstOrDefault(p => p.Value.CurrentStep == "Pending");
		if (pending.Key is null)
		{
			return;
		}

		var otherHosts = (from p in _contextList
							where p.Value.ServiceInfo.ServiceName == pending.Value.ServiceInfo.ServiceName
							&& p.Value.HostName != pending.Value.HostName
							&& p.Value.CurrentStep != "Pending"
							select p).ToList();

			// One update by host for the same service
		if (!otherHosts.Any())
		{
			pending.Value.CurrentStep = "Starting";
			Task.Run(() => base.ProcessUpdate(pending.Value, cancellationToken));
		}
	}
}
