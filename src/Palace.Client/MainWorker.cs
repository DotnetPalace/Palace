using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using ArianeBus;

using Azure.Core;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Palace.Shared;
using Palace.Shared.Messages;

namespace Palace.Client;

public class MainWorker : BackgroundService
{
    private readonly ILogger<MainWorker> _logger;
	private readonly PalaceSettings _settings;
	private readonly IServiceBus _bus;

	public MainWorker(ILogger<MainWorker> logger,
        PalaceSettings settings,
		ArianeBus.IServiceBus bus)
    {
        _logger = logger;
		_settings = settings;
		_bus = bus;
	}

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
		var entryAssembly = Assembly.GetEntryAssembly()!;
		var version = $"{entryAssembly.GetName().Version}";
		var productAttribute = entryAssembly.GetCustomAttribute<System.Reflection.AssemblyProductAttribute>();
		var fileInfo = new System.IO.FileInfo(entryAssembly.Location);
		var startedDate = DateTime.Now;

		while (!stoppingToken.IsCancellationRequested)
		{
			var rmi = new RunningMicroserviceInfo
			{
				ServiceName = _settings.ServiceName,
				Version = version,
				Location = fileInfo.FullName,
				UserInteractive = System.Environment.UserInteractive,
				LastWriteTime = fileInfo.LastWriteTime,
				ThreadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count,
				ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
				StartedDate = startedDate,
				CommandLine = System.Environment.CommandLine,
				EnvironmentName = _settings.HostEnvironmentName,
				ServiceState = ServiceState.Running,
				WorkingSet = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64,
			};
			await _bus.EnqueueMessage(_settings.ServiceHealthQueueName, new Shared.Messages.ServiceHealthCheck
			{
				HostName = _settings.HostName,
				ServiceInfo = rmi
			});

			_logger.LogTrace("Worker running at: {time}", DateTimeOffset.Now);
			if (!stoppingToken.IsCancellationRequested)
			{
				await Task.Delay(_settings.ScanIntervalInSeconds * 1000, stoppingToken);
			}
		}
    }

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Service stopping");

		// Send a message to the bus to say that the service is stopping
		await _bus.EnqueueMessage(_settings.StopServiceReportQueueName, new StopServiceReport
		{
			ServiceName = _settings.ServiceName,
			HostName = _settings.HostName,
			State = ServiceState.Offline
		});

		await base.StopAsync(cancellationToken);
	}

}
