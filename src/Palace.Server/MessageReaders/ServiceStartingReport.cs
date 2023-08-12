using Palace.Server.Services;
using Palace.Shared.Messages;

namespace Palace.Server.MessageReaders;

public class ServiceStartingReport : ArianeBus.MessageReaderBase<Palace.Shared.Messages.StartingServiceReport>
{
	private readonly Orchestrator _orchestrator;
    private readonly ILogger<ServiceStartingReport> _logger;

    public ServiceStartingReport(Orchestrator orchestrator,
		ILogger<ServiceStartingReport> logger)
    {
		_orchestrator = orchestrator;
        _logger = logger;
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

        var rmi = new Models.ExtendedMicroServiceInfo
		{
			ServiceName = message.ServiceName,
            HostName = message.HostName,
			Location = message.InstallationFolder,
			ProcessId = message.ProcessId,
			ServiceState = message.ServiceState,
			CommandLine = $"{message.CommandLine}",
            FailReason = message.FailReason
		};

		_orchestrator.AddOrUpdateMicroServiceInfo(rmi);
	}
}
