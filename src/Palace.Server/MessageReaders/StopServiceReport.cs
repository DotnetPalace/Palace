using Palace.Server.Services;

namespace Palace.Server.MessageReaders;

public class StopServiceReport : ArianeBus.MessageReaderBase<Palace.Shared.Messages.StopServiceReport>
{
	private readonly Orchestrator _orchestrator;
    private readonly ILogger<StopServiceReport> _logger;

    public StopServiceReport(Services.Orchestrator orchestrator,
		ILogger<StopServiceReport> logger)
    {
		_orchestrator = orchestrator;
        _logger = logger;
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

        var microServiceInfo = _orchestrator.GetServiceList()
								 .Where(i => i.ServiceName == message.ServiceName
										&& i.HostName == message.HostName)
								 .SingleOrDefault();

		if (microServiceInfo is not null)
		{
			microServiceInfo.ServiceState = message.State;
			microServiceInfo.LastHitDate = message.ActionDate;
			_orchestrator.AddOrUpdateMicroServiceInfo(microServiceInfo);
		}
	}
}
