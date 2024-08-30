namespace Palace.Host.MessageReaders;

internal class KillService(
    ILogger<KillService> logger,
    Configuration.GlobalSettings settings,
    ArianeBus.IServiceBus bus,
    IProcessHelper processHelper
    )
    : ArianeBus.MessageReaderBase<Palace.Shared.Messages.KillService>
{
    public override Task ProcessMessageAsync(Shared.Messages.KillService message, CancellationToken cancellationToken)
    {
		if (message is null)
        {
            logger.LogError("message is null");
            return Task.CompletedTask;
        }

        if (message.Timeout < DateTime.Now)
        {
            logger.LogTrace("message is too old");
            return Task.CompletedTask;
        }

        if (!message.HostName.Equals(settings.HostName))
        {
            logger.LogTrace("installation service not for me");
            return Task.CompletedTask;
        }

        Task.Run(() => ProcessMessageInternal(message, cancellationToken));
        return Task.CompletedTask;
    }

    async Task ProcessMessageInternal(Shared.Messages.KillService message, CancellationToken cancellationToken)
    {
		var report = new Shared.Messages.KillServiceReport
		{
			ActionSourceId = message.ActionId,
			HostName = settings.HostName,
			ServiceName = message.ServiceSettings.ServiceName,
		};

		try
		{
			var result = await processHelper.KillProcess(message.ProcessId);
			report.Success = result.Success;
		}
		catch (Exception ex)
		{
			report.Success = false;
			report.FailReason = ex.Message;
		}

		await bus.EnqueueMessage(settings.KillServiceReportQueueName, report, cancellationToken: cancellationToken);
	}
}