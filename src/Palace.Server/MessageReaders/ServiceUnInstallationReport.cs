using Palace.Server.Services;

namespace Palace.Server.MessageReaders;

public class ServiceUnInstallationReport(
	Orchestrator orchestrator,
	ILogger<ServiceUnInstallationReport> logger,
	Services.LongActionService longActionService
	) 
	: ArianeBus.MessageReaderBase<Palace.Shared.Messages.ServiceUnInstallationReport>
{
	public override async Task ProcessMessageAsync(Shared.Messages.ServiceUnInstallationReport message, CancellationToken cancellationToken)
	{
		await Task.Yield();

		if (message is null)
		{
			logger.LogError("message is null");
			return;
		}

		if (message.Timeout < DateTime.Now)
		{
			logger.LogTrace("message is too old");
			return;
		}

		// On recherche le service 
		var key = $"{message.HostName}__{message.ServiceName}".ToLower();
		var emsi = orchestrator.GetExtendedMicroServiceInfoByKey(key);

		if (emsi is null)
		{
			logger.LogError("Service {serviceNAme} not found in host {hostName}", message.ServiceName, message.HostName);
			return;
		}

		if (!message.Success)
		{
			logger.LogError("Service {serviceNAme} not uninstalled in host {hostName}", message.ServiceName, message.HostName);
			emsi.FailReason = message.FailReason;
			orchestrator.AddOrUpdateMicroServiceInfo(emsi);

			await longActionService.SetActionCompleted(new Models.ActionResult
			{
				ActionId = message.ActionSourceId,
				Success = false,
				FailReason = message.FailReason
			});

			return;
		}

		// On supprime le service de la liste
		orchestrator.RemoveMicroServiceInfo(emsi);

		await longActionService.SetActionCompleted(new Models.ActionResult
		{
			ActionId = message.ActionSourceId,
			Success = true,
		});
	}
}
