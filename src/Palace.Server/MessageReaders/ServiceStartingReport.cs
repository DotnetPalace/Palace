using System.Diagnostics;

using Palace.Server.Services;
using Palace.Shared.Messages;

namespace Palace.Server.MessageReaders;

public class ServiceStartingReport : ArianeBus.MessageReaderBase<Palace.Shared.Messages.StartingServiceReport>
{
	private readonly Orchestrator _orchestrator;
    private readonly ILogger<ServiceStartingReport> _logger;
	private readonly LongActionService _longActionService;

	public ServiceStartingReport(Orchestrator orchestrator,
		ILogger<ServiceStartingReport> logger,
        LongActionService longActionService)
    {
		_orchestrator = orchestrator;
        _logger = logger;
		_longActionService = longActionService;
	}

    public override async Task ProcessMessageAsync(StartingServiceReport message, CancellationToken cancellationToken)
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

        var key = $"{message.HostName}-{message.ServiceName}".ToLower();
        var emsi = _orchestrator.GetExtendedMicroServiceInfoByKey(key);
        if (emsi is not null)
        {
            emsi.ServiceState = message.ServiceState;
            emsi.CommandLine = $"{message.CommandLine}";
            emsi.FailReason = message.FailReason;
            emsi.ProcessId = message.ProcessId;
            emsi.Location = message.InstallationFolder;
		}
		else
        {
			emsi = new Models.ExtendedMicroServiceInfo
			{
				ServiceName = message.ServiceName,
				HostName = message.HostName,
				Location = message.InstallationFolder,
				ProcessId = message.ProcessId,
				ServiceState = message.ServiceState,
				CommandLine = $"{message.CommandLine}",
				FailReason = message.FailReason
			};
		}

		_orchestrator.AddOrUpdateMicroServiceInfo(emsi);

		var actionResult = new Models.ActionResult
		{
			ActionId = message.ActionSourceId,
			Success = message.ServiceState == ServiceState.Running,
			FailReason = message.FailReason
		};
		await _longActionService.SetActionCompleted(actionResult);
		_orchestrator.OnLongActionProgress(actionResult);
	}
}
