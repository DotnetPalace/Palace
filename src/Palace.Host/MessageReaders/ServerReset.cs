namespace Palace.Host.MessageReaders;

internal class ServerReset(
    ILogger<ServerReset> logger,
    Configuration.GlobalSettings settings,
    ArianeBus.IServiceBus bus,
    IProcessHelper processHelper
    )
    : ArianeBus.MessageReaderBase<Palace.Shared.Messages.ServerReset>
{
    public override async Task ProcessMessageAsync(Shared.Messages.ServerReset message, CancellationToken cancellationToken)
    {
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

        var installedServiceList = await processHelper.GetInstalledServiceList(settings.InstallationFolder);
        var serviceSettingsList = installedServiceList.Select(x => x.MainAssembly).ToList();

        logger.LogInformation("{Count} found already installed services", installedServiceList.Count);

        var runningServiceList = processHelper.GetRunningProcess(serviceSettingsList.ToArray());
        foreach (var item in runningServiceList)
        {
            installedServiceList.RemoveAll(i => i.ServiceName == i.ServiceName!);
        }

        logger.LogInformation("{Count} found already running services", runningServiceList.Count);

        foreach (var serviceSettings in installedServiceList)
        {
            var report = new Shared.Messages.ServiceInstallationReport
            {
                HostName = settings.HostName,
                ServiceName = serviceSettings.ServiceName,
                Success = true
            };
            await bus.EnqueueMessage(settings.InstallationReportQueueName, report);
        }
    }
}
