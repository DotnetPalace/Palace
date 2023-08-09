using Palace.Server.Services;

namespace Palace.Server.MessageReaders;

public class HostHealthCheck : ArianeBus.MessageReaderBase<Palace.Shared.Messages.HostHealthCheck>
{
	private readonly Orchestrator _orchestrator;
    private readonly ILogger<HostHealthCheck> _logger;

    public HostHealthCheck(Services.Orchestrator orchestrator,
		ILogger<HostHealthCheck> logger)
    {
		_orchestrator = orchestrator;
        _logger = logger;
    }

    public override async Task ProcessMessageAsync(Shared.Messages.HostHealthCheck message, CancellationToken cancellationToken)
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

        var hostInfo = new Models.HostInfo
		{
			HostName = message.HostName,
			MachineName = message.MachineName,
			ExternalIp = message.ExternalIp,
			LastHitDate = message.Now,
			Version = message.Version,
            HostState = HostState.Running
		};
		_orchestrator.AddOrUpdateHost(hostInfo);
	}
}
