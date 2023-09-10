using System.Runtime;

using ArianeBus;

using Palace.Host.Configuration;

namespace Palace.Host.MessageReaders;

internal class KillService : ArianeBus.MessageReaderBase<Palace.Shared.Messages.KillService>
{
	private readonly ILogger<KillService> _logger;
	private readonly GlobalSettings _settings;
	private readonly IServiceBus _bus;

	public KillService(ILogger<KillService> logger,
		Configuration.GlobalSettings _settings,
		ArianeBus.IServiceBus bus)
    {
		_logger = logger;
		this._settings = _settings;
		_bus = bus;
	}
    public override async Task ProcessMessageAsync(Shared.Messages.KillService message, CancellationToken cancellationToken)
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

		if (!message.HostName.Equals(_settings.HostName))
		{
			_logger.LogTrace("installation service not for me");
			return;
		}

		var report = new Shared.Messages.KillServiceReport
		{
			ActionSourceId = message.ActionId,
			HostName = _settings.HostName,
			ServiceName = message.ServiceSettings.ServiceName,
		};

		var result = await ProcessHelper.KillProcess(message.ProcessId);
		report.Success = result.Success;
		report.FailReason = report.FailReason;

		await _bus.EnqueueMessage(_settings.KillServiceReportQueueName, report, cancellationToken: cancellationToken);
	}
}
