using Palace.Server.Services;

namespace Palace.Server.MessageReaders;
public class KillServiceReport(
    Orchestrator orchestrator,
    ILogger<KillServiceReport> logger
    )
    : ArianeBus.MessageReaderBase<Palace.Shared.Messages.KillServiceReport>
{
    public override async Task ProcessMessageAsync(Palace.Shared.Messages.KillServiceReport message, CancellationToken cancellationToken)
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
        if (emsi is null)
        {
            return;
        }

        if (message.Success)
        {
            emsi.ServiceState = ServiceState.Down;
        }
    }
}
