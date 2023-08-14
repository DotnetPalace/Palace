using Palace.Server.Services;

namespace Palace.Server.MessageReaders;

public class ServiceUnInstallationReport : ArianeBus.MessageReaderBase<Palace.Shared.Messages.ServiceUnInstallationReport>
{
	private readonly Orchestrator _orchestrator;
	private readonly ILogger<ServiceUnInstallationReport> _logger;
	private readonly LongActionService _longActionService;

	public ServiceUnInstallationReport(Orchestrator orchestrator,
		ILogger<ServiceUnInstallationReport> logger,
		Services.LongActionService longActionService)
    {
		_orchestrator = orchestrator;
		_logger = logger;
		_longActionService = longActionService;
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
		var key = $"{message.HostName}-{message.ServiceName}".ToLower();
		var emsi = _orchestrator.GetExtendedMicroServiceInfoByKey(key);

		if (emsi is null)
		{
			_logger.LogError("Service {serviceNAme} not found in host {hostName}", message.ServiceName, message.HostName);
			return;
		}

		if (!message.Success)
		{
			_logger.LogError("Service {serviceNAme} not uninstalled in host {hostName}", message.ServiceName, message.HostName);
			emsi.FailReason = message.FailReason;
			_orchestrator.AddOrUpdateMicroServiceInfo(emsi);

			await _longActionService.SetActionCompleted(new Models.ActionResult
			{
				ActionId = message.ActionSourceId,
				Success = false,
				FailReason = message.FailReason
			});

			return;
		}

		// On supprime le service de la liste
		_orchestrator.RemoveMicroServiceInfo(emsi);

		await _longActionService.SetActionCompleted(new Models.ActionResult
		{
			ActionId = message.ActionSourceId,
			Success = true,
		});
	}
}
