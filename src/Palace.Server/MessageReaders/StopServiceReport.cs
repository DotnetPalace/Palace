using Palace.Server.Services;

namespace Palace.Server.MessageReaders;

public class StopServiceReport(
	Services.Orchestrator orchestrator,
    ILogger<StopServiceReport> logger,
    LongActionService longActionService
	) 
	: ArianeBus.MessageReaderBase<Palace.Shared.Messages.StopServiceReport>
{
    public override async Task ProcessMessageAsync(Shared.Messages.StopServiceReport message, CancellationToken cancellationToken)
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

		var key = $"{message.HostName}__{message.ServiceName}";
		var microServiceInfo = orchestrator.GetExtendedMicroServiceInfoByKey(key);

		if (microServiceInfo is not null)
		{
			microServiceInfo.ServiceState = message.State;
			microServiceInfo.LastHitDate = message.ActionDate;
			orchestrator.AddOrUpdateMicroServiceInfo(microServiceInfo);
		}

		if (message.Origin != "Recycle")
		{
			var actionResult = new Models.ActionResult
			{
				ActionId = message.ActionSourceId,
				Success = message.State == ServiceState.TryToStop,
				StepName = "StopService",
			};
			await longActionService.SetActionCompleted(actionResult);
			orchestrator.OnLongActionProgress(actionResult);
		}
		else if (message.Origin == "Recycle")
		{
			await longActionService.AddLog(message.ActionSourceId, new Models.ActionLog
			{
				Message = message.State == ServiceState.TryToStop ? "Service stopped" : "not stopped"
			});
        }

    }
}
