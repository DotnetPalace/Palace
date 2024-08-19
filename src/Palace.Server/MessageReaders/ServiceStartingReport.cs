using Palace.Server.Services;
using Palace.Shared.Messages;

namespace Palace.Server.MessageReaders;

public class ServiceStartingReport(
    Orchestrator orchestrator,
    ILogger<ServiceStartingReport> logger,
    LongActionService longActionService
    )
    : ArianeBus.MessageReaderBase<Palace.Shared.Messages.StartingServiceReport>
{
    public override async Task ProcessMessageAsync(StartingServiceReport message, CancellationToken cancellationToken)
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

        var key = $"{message.HostName}__{message.ServiceName}".ToLower();
        var emsi = orchestrator.GetExtendedMicroServiceInfoByKey(key);
        if (emsi is not null)
        {
            emsi.ServiceState = message.ServiceState;
            emsi.CommandLine = $"{message.CommandLine}";
            emsi.FailReason = message.FailReason;
            emsi.ProcessId = message.ProcessId;
            emsi.Location = message.InstallationFolder;
            emsi.RemovedDate = null;
        }
        else
        {
            emsi = new Models.ExtendedMicroServiceInfo
            {
                ServiceName = message.ServiceName,
                HostName = message.HostName,
                Location = message.InstallationFolder,
                ProcessId = message.ProcessId,
                ServiceState = message.ServiceState,
                CommandLine = $"{message.CommandLine}",
                FailReason = message.FailReason
            };
        }

        orchestrator.AddOrUpdateMicroServiceInfo(emsi);

        var actionResult = new Models.ActionResult
        {
            ActionId = message.ActionSourceId,
            Success = message.ServiceState == ServiceState.Running,
            FailReason = message.FailReason
        };
        await longActionService.SetActionCompleted(actionResult);
        orchestrator.OnLongActionProgress(actionResult);
    }
}
