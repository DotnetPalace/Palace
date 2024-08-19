namespace Palace.Server.MessageReaders;

public class ServiceHealthCheck(
    Services.Orchestrator orchestrator,
    ILogger<ServiceHealthCheck> logger
    )
    : ArianeBus.MessageReaderBase<Palace.Shared.Messages.ServiceHealthCheck>
{
    public override async Task ProcessMessageAsync(Palace.Shared.Messages.ServiceHealthCheck message, CancellationToken cancellationToken)
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

        var emsi = new Models.ExtendedMicroServiceInfo
        {
            ServiceName = message.ServiceInfo.ServiceName,
            HostName = message.HostName,
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

        orchestrator.AddOrUpdateMicroServiceInfo(emsi);
    }
}
