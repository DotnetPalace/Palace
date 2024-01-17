using Palace.Server.Services;

namespace Palace.Server.MessageReaders;

public class StopServiceReport : ArianeBus.MessageReaderBase<Palace.Shared.Messages.StopServiceReport>
{
	private readonly Orchestrator _orchestrator;
    private readonly ILogger<StopServiceReport> _logger;
	private readonly LongActionService _longActionService;

	public StopServiceReport(Services.Orchestrator orchestrator,
		ILogger<StopServiceReport> logger,
        LongActionService longActionService)
    {
		_orchestrator = orchestrator;
        _logger = logger;
		_longActionService = longActionService;
	}

    public override async Task ProcessMessageAsync(Shared.Messages.StopServiceReport message, CancellationToken cancellationToken)
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

		var key = $"{message.HostName}__{message.ServiceName}";
		var microServiceInfo = _orchestrator.GetExtendedMicroServiceInfoByKey(key);

		if (microServiceInfo is not null)
		{
			microServiceInfo.ServiceState = message.State;
			microServiceInfo.LastHitDate = message.ActionDate;
			_orchestrator.AddOrUpdateMicroServiceInfo(microServiceInfo);
		}

		if (message.Origin != "Recycle")
		{
			var actionResult = new Models.ActionResult
			{
				ActionId = message.ActionSourceId,
				Success = message.State == ServiceState.TryToStop,
				StepName = "StopService",
			};
			await _longActionService.SetActionCompleted(actionResult);
			_orchestrator.OnLongActionProgress(actionResult);
		}
		else if (message.Origin == "Recycle")
		{
			await _longActionService.AddLog(message.ActionSourceId, new Models.ActionLog
			{
				Message = message.State == ServiceState.TryToStop ? "Service stopped" : "not stopped"
			});
        }

    }
}
