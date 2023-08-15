using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

using ArianeBus;

using Microsoft.Extensions.Logging;

using Palace.Host.Configuration;

namespace Palace.Host.MessageReaders;

internal class ServerReset : ArianeBus.MessageReaderBase<Palace.Shared.Messages.ServerReset>
{
	private readonly ILogger<ServerReset> _logger;
	private readonly GlobalSettings _settings;
	private readonly IServiceBus _bus;

	public ServerReset(ILogger<ServerReset> logger,
		Configuration.GlobalSettings settings,
		ArianeBus.IServiceBus bus)
    {
		_logger = logger;
		_settings = settings;
		_bus = bus;
	}

    public override async Task ProcessMessageAsync(Shared.Messages.ServerReset message, CancellationToken cancellationToken)
	{
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

		var installedServiceList = await ProcessHelper.GetInstalledServiceList(_settings.InstallationFolder);
		var serviceSettingsList = installedServiceList.Select(x => x.MainAssembly).ToList();

		_logger.LogInformation("{count} found already installed services", installedServiceList.Count);

		var runningServiceList = ProcessHelper.GetRunningProcess(serviceSettingsList.ToArray());
		foreach (var item in runningServiceList)
		{
			installedServiceList.RemoveAll(i => i.ServiceName == i.ServiceName);
		}

		_logger.LogInformation("{count} found already running services", runningServiceList.Count);

		foreach (var serviceSettings in installedServiceList)
		{
			var report = new Shared.Messages.ServiceInstallationReport
			{
				HostName = _settings.HostName,
				ServiceName = serviceSettings.ServiceName,
				Success = true
			};
			await _bus.EnqueueMessage(_settings.InstallationReportQueueName, report);
		}
	}
}
