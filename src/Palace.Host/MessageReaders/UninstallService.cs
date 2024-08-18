using Palace.Shared.Messages;

namespace Palace.Host.MessageReaders;

internal class UninstallService(
    ILogger<UninstallService> logger,
    Configuration.GlobalSettings settings,
    ArianeBus.IServiceBus bus,
    IProcessHelper processHelper
    )
    : ArianeBus.MessageReaderBase<Palace.Shared.Messages.UnInstallService>
{

    public override async Task ProcessMessageAsync(UnInstallService message, CancellationToken cancellationToken)
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

        if (!message.HostName.Equals(settings.HostName))
        {
            logger.LogTrace("installation service not for me");
            return;
        }

        var report = new Shared.Messages.ServiceUnInstallationReport
        {
            HostName = settings.HostName,
            ServiceName = message.ServiceSettings.ServiceName,
            ActionSourceId = message.ActionId
        };

        var commandLine = $"{message.ServiceSettings.MainAssembly} {message.ServiceSettings.Arguments}".Trim();
        // On verifie si le service est déjà en cours
        var runningServiceList = processHelper.GetRunningProcess(commandLine);
        if (runningServiceList.Count > 0)
        {
            logger.LogTrace("service is running");
            report.Success = false;
            report.FailReason = $"Service {message.ServiceSettings.ServiceName} is running";
            await bus.EnqueueMessage(settings.UnInstallationReportQueueName, report);
            return;
        }

        // On verifie que le service est déjà installé
        var installationFolder = System.IO.Path.Combine(settings.InstallationFolder, message.ServiceSettings.ServiceName);
        if (!System.IO.Directory.Exists(installationFolder))
        {
            logger.LogTrace("service is not installed");
            report.Success = false;
            report.FailReason = $"Service {message.ServiceSettings.ServiceName} is not installed";
            await bus.EnqueueMessage(settings.UnInstallationReportQueueName, report);
            return;
        }

        var count = 0;
        while (true)
        {
            // On supprime le dossier d'installation
            try
            {
                System.IO.Directory.Delete(installationFolder, true);
                report.Success = true;
                break;
            }
            catch (Exception ex)
            {
                if (count > 2)
                {
                    logger.LogError(ex, "Error when deleting folder {InstallationFolder}", installationFolder);
                    report.Success = false;
                    report.FailReason = $"Error when deleting folder {installationFolder}";
                    break;
                }
                count++;
                await Task.Delay(2 * 1000);
            }
        }

        await bus.EnqueueMessage(settings.UnInstallationReportQueueName, report);
    }
}