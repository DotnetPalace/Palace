namespace Palace.Server.Services.UpdateStrategies;

public class ByServiceUpdateStrategy : UpdateStrategyBase
{
	public ByServiceUpdateStrategy(IServiceScopeFactory serviceScopeFactory,
		ILogger<ByServiceUpdateStrategy> logger,
		Orchestrator orchestrator)
		: base(serviceScopeFactory, logger, orchestrator)
	{

	}

	override public string Name => "ByService";

	public override void ProcessNextUpdate(CancellationToken cancellationToken)
	{
		var pending = _contextList.FirstOrDefault(p => p.Value.CurrentStep == "Pending");
		if (pending.Key is null)
		{
			return;
		}

		var otherServices = (from ctx in _contextList
							  where ctx.Value.ServiceInfo.ServiceName == pending.Value.ServiceInfo.ServiceName
								 && ctx.Value.CurrentStep != "Pending"
							  select ctx).ToList();

		if (!otherServices.Any())
		{
			pending.Value.CurrentStep = "Starting";
			Task.Run(() => base.ProcessUpdate(pending.Value, cancellationToken));
		}
	}
}
