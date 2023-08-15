namespace Palace.Server.Services.UpdateStrategies;

public class ChaosUpdateStrategy : UpdateStrategyBase
{
	public ChaosUpdateStrategy(IServiceScopeFactory serviceScopeFactory,
		ILogger<ChaosUpdateStrategy> logger,
		Orchestrator orchestrator)
		: base(serviceScopeFactory, logger, orchestrator)
	{

	}

	public override string Name => "Chaos";

	public override void ProcessNextUpdate(CancellationToken cancellationToken)
	{
		var toStartList = (from p in _contextList
						  where p.Value.CurrentStep == "Pending"
						  select p).ToList();

		foreach (var item in toStartList)
		{
			item.Value.CurrentStep = "Starting";
			Task.Run(() => base.ProcessUpdate(item.Value, cancellationToken));
		}
	}
}
