namespace Palace.Server.MessageReaders;

public class HostHealthCheck(
    Services.Orchestrator orchestrator,
    ILogger<HostHealthCheck> logger
    )
    : ArianeBus.MessageReaderBase<Palace.Shared.Messages.HostHealthCheck>
{
    public override async Task ProcessMessageAsync(Shared.Messages.HostHealthCheck message, CancellationToken cancellationToken)
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

        var hostInfo = new Models.HostInfo
        {
            HostName = message.HostName,
            MachineName = message.MachineName,
            ExternalIp = message.ExternalIp,
            LastHitDate = message.Now,
            Version = message.Version,
            HostState = HostState.Running,
            MainFileName = message.MainFileName,
            TotalDriveSize = message.TotalDriveSize,
            TotalFreeSpaceOfDriveSize = message.TotalFreeSpaceOfDriveSize,
            OsDescription = message.OsDescription,
            OsVersion = message.OsVersion,
            ProcessId = message.ProcessId,
            PercentCpu = message.PercentCpu
        };
        orchestrator.AddOrUpdateHost(hostInfo);
    }
}
