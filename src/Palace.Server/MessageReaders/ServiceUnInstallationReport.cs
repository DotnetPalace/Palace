using Palace.Server.Services;

namespace Palace.Server.MessageReaders;

public class ServiceUnInstallationReport : ArianeBus.MessageReaderBase<Palace.Shared.Messages.ServiceUnInstallationReport>
{
	private readonly Orchestrator _orchestrator;
	private readonly ILogger<ServiceUnInstallationReport> _logger;

	public ServiceUnInstallationReport(Orchestrator orchestrator,
		ILogger<ServiceUnInstallationReport> logger)
    {
		_orchestrator = orchestrator;
		_logger = logger;
	}

    public override async Task ProcessMessageAsync(Shared.Messages.ServiceUnInstallationReport message, CancellationToken cancellationToken)
	{
		await Task.Yield();

		if (message is null)
		{
			_logger.LogError("message is null");
			return;
		}

		if (message.Timeout < DateTime.Now)
		{
			_logger.LogTrace("message is too old");
			return;
		}

		// On recherche le service 
		var rmi = _orchestrator.GetServiceList()
					.Where(x => x.ServiceName == message.ServiceName
						&& x.HostName == message.HostName)
					.SingleOrDefault();

		if (rmi is null)
		{
			_logger.LogError("Service {serviceNAme} not found in host {hostName}", message.ServiceName, message.HostName);
			return;
		}

		if (!message.Success)
		{
			_logger.LogError("Service {serviceNAme} not uninstalled in host {hostName}", message.ServiceName, message.HostName);
			rmi.FailReason = message.FailReason;
			_orchestrator.AddOrUpdateMicroServiceInfo(rmi);
			return;
		}

		// On supprime le service de la liste
		_orchestrator.RemoveMicroServiceInfo(rmi);
	}
}
