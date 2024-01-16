using Microsoft.AspNetCore.Mvc;

using Palace.Server.Services;

namespace Palace.Server.MessageReaders;
public class KillServiceReport : ArianeBus.MessageReaderBase<Palace.Shared.Messages.KillServiceReport>
{
	private readonly Orchestrator _orchestrator;
	private readonly ILogger<KillServiceReport> _logger;

	public KillServiceReport(Orchestrator orchestrator,
		ILogger<KillServiceReport> logger)
    {
		_orchestrator = orchestrator;
		_logger = logger;
	}

    public override async Task ProcessMessageAsync(Palace.Shared.Messages.KillServiceReport message, CancellationToken cancellationToken)
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

		var key = $"{message.HostName}||{message.ServiceName}".ToLower();
		var emsi = _orchestrator.GetExtendedMicroServiceInfoByKey(key);
		if (emsi is null)
		{
			return;
		}

		if (message.Success)
		{
			emsi.ServiceState = ServiceState.Down;
		}
	}
}
