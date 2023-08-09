namespace Palace.Server.Services;

public class HealthCheckerService : BackgroundService
{
	private readonly Orchestrator _orchestrator;

	public HealthCheckerService(Orchestrator orchestrator)
    {
		_orchestrator = orchestrator;
	}

    public override Task StartAsync(CancellationToken cancellationToken)
	{
		return base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			CheckHosts();
			CheckServices();
			await Task.Delay(10 * 1000, stoppingToken);
		}
	}

	public void CheckServices()
	{
		var services = _orchestrator.GetServiceList();
		foreach (var service in services)
		{
			if (service.LastHitDate < DateTime.Now.AddMinutes(-1))
			{
				service.ServiceState = Palace.Shared.ServiceState.Down;
				_orchestrator.AddOrUpdateMicroServiceInfo(service);
			}
		}
	}

	public void CheckHosts()
	{
		var hosts = _orchestrator.GetHostList();
		foreach (var host in hosts)
		{
			if (host.LastHitDate < DateTime.Now.AddMinutes(-1))
			{
				host.HostState = Palace.Shared.HostState.Down;
				_orchestrator.AddOrUpdateHost(host);
			}
		}
	}
}
