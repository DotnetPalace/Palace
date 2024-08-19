namespace Palace.Server.MessageReaders;

public class ServiceInstallationReport(
    Services.Orchestrator orchestrator,
    ILogger<ServiceInstallationReport> logger,
    Services.LongActionService longActionService
    )
    : ArianeBus.MessageReaderBase<Palace.Shared.Messages.ServiceInstallationReport>
{
    public override async Task ProcessMessageAsync(Shared.Messages.ServiceInstallationReport message, CancellationToken cancellationToken)
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

        var serviceInfo = new Models.ExtendedMicroServiceInfo();
        serviceInfo.HostName = message.HostName;
        serviceInfo.ServiceName = message.ServiceName;
        serviceInfo.Location = message.InstallationFolder;
        if (!message.Success)
        {
            serviceInfo.ServiceState = ServiceState.InstallationFailed;
            serviceInfo.FailReason = message.FailReason;
            var actionResult = new Models.ActionResult
            {
                ActionId = message.ActionSourceId,
                Success = false,
                FailReason = message.FailReason
            };
            await longActionService.SetActionCompleted(actionResult);
            orchestrator.OnLongActionProgress(actionResult);
        }
        else
        {
            if (message.Trigger == "FromUpdate")
            {
                serviceInfo.ServiceState = ServiceState.Updated;
            }
            else
            {
                serviceInfo.ServiceState = ServiceState.Offline;
            }
            var actionResult = new Models.ActionResult
            {
                ActionId = message.ActionSourceId,
                Success = true
            };
            await longActionService.SetActionCompleted(actionResult);
            orchestrator.OnLongActionProgress(actionResult);
        }

        orchestrator.AddOrUpdateMicroServiceInfo(serviceInfo);

    }

}

