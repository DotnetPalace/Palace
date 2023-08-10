﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

using ArianeBus;

using Palace.Host.Configuration;
using Palace.Shared.Messages;

namespace Palace.Host.MessageReaders;

internal class UninstallService : ArianeBus.MessageReaderBase<Palace.Shared.Messages.UnInstallService>
{
	private readonly ILogger<UninstallService> _logger;
	private readonly GlobalSettings _settings;
	private readonly IServiceBus _bus;

	public UninstallService(ILogger<UninstallService> logger,
		Configuration.GlobalSettings settings,
		ArianeBus.IServiceBus bus)
    {
		_logger = logger;
		_settings = settings;
		_bus = bus;
	}

    public override async Task ProcessMessageAsync(UnInstallService message, CancellationToken cancellationToken)
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

		var report = new Shared.Messages.ServiceUnInstallationReport
		{
			HostName = _settings.HostName,
			ServiceName = message.ServiceSettings.ServiceName,
		};

		// On verifie si le service est déjà en cours
		var runningServiceList = ProcessHelper.GetRunningProcess(message.ServiceSettings.MainAssembly);
		if (runningServiceList.Count > 0)
		{
			_logger.LogTrace("service is running");
			report.Success = false;
			report.FailReason = $"Service {message.ServiceSettings.ServiceName} is running";
			await _bus.EnqueueMessage(_settings.UnInstallationReportQueueName, report);
			return;
		}

		// On verifie que le service est déjà installé
		var installationFolder = System.IO.Path.Combine(_settings.InstallationFolder, message.ServiceSettings.ServiceName);
		if (!System.IO.Directory.Exists(installationFolder))
		{
			_logger.LogTrace("service is not installed");
			report.Success = false;
			report.FailReason = $"Service {message.ServiceSettings.ServiceName} is not installed";
			await _bus.EnqueueMessage(_settings.UnInstallationReportQueueName, report);
			return;
		}

		// On supprime le dossier d'installation
		try
		{
			System.IO.Directory.Delete(installationFolder, true);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, $"Error when deleting folder {installationFolder}");
			report.Success = false;
			report.FailReason = $"Error when deleting folder {installationFolder}";
			await _bus.EnqueueMessage(_settings.UnInstallationReportQueueName, report);
		}

		report.Success = true;
		await _bus.EnqueueMessage(_settings.UnInstallationReportQueueName, report);
	}
}
