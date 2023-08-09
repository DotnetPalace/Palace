using Palace.Server.Services;

namespace Palace.Server.MessageReaders;

public class ServiceHealthCheck : ArianeBus.MessageReaderBase<Palace.Shared.Messages.ServiceHealthCheck>
{
	private readonly Orchestrator _orchestrator;
    private readonly ILogger<ServiceHealthCheck> _logger;

    public ServiceHealthCheck(Services.Orchestrator orchestrator,
		ILogger<ServiceHealthCheck> logger)
    {
		_orchestrator = orchestrator;
        _logger = logger;
    }

    public override async Task ProcessMessageAsync(Palace.Shared.Messages.ServiceHealthCheck message, CancellationToken cancellationToken)
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
			ServiceName = message.ServiceInfo.ServiceName,
			Version = message.ServiceInfo.Version,
			Location = message.ServiceInfo.Location,
			UserInteractive = message.ServiceInfo.UserInteractive,
			LastWriteTime = message.ServiceInfo.LastWriteTime,
			ThreadCount = message.ServiceInfo.ThreadCount,
			ProcessId = message.ServiceInfo.ProcessId,
			ServiceState = message.ServiceInfo.ServiceState,
			StartedDate = message.ServiceInfo.StartedDate,
			CommandLine = message.ServiceInfo.CommandLine,
			PeakWorkingSet = message.ServiceInfo.PeakWorkingSet,
			WorkingSet = message.ServiceInfo.WorkingSet,
			PeakPagedMem = message.ServiceInfo.PeakPagedMem,
			PeakVirtualMem = message.ServiceInfo.PeakVirtualMem,
			EnvironmentName = message.ServiceInfo.EnvironmentName,
			LastHitDate = DateTime.Now,
		};
		rmi.HostName = message.HostName;

		_orchestrator.AddOrUpdateMicroServiceInfo(rmi);
	}
}
