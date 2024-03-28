namespace Palace.Server.Services;

public class HealthCheckerService(
	Orchestrator orchestrator,
	ILogger<HealthCheckerService> logger
	) 
	: BackgroundService
{
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
		var services = orchestrator.GetServiceList();
		foreach (var service in services)
		{
			if (!service.LastHitDate.HasValue
				|| service.LastHitDate < DateTime.Now.AddMinutes(-1))
			{
				logger.LogWarning("Service {serviceName} is down", service.ServiceName);
				service.ServiceState = Palace.Shared.ServiceState.Down;
				orchestrator.AddOrUpdateMicroServiceInfo(service);
			}
		}
	}

	public void CheckHosts()
	{
		var hosts = orchestrator.GetHostList();
		foreach (var host in hosts)
		{
			if (host.LastHitDate < DateTime.Now.AddMinutes(-1))
			{
				logger.LogWarning("Host {hostName} is down", host.HostName);
				host.HostState = Palace.Shared.HostState.Down;
				orchestrator.AddOrUpdateHost(host);
			}
		}
	}
}
