﻿using ArianeBus;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Palace.Shared.Messages;

namespace Palace.Client;

public sealed class StopMessageReader(
    PalaceSettings settings,
	ILogger<StopMessageReader> logger,
	IServiceBus bus,
	IHostApplicationLifetime hostApplicationLifetime
    ) 
    : MessageReaderBase<StopService>
{
	private System.Timers.Timer _timer = default!;

	public override async Task ProcessMessageAsync(StopService message, CancellationToken cancellationToken)
    {
        if (message is null)
        {
            logger.LogWarning("message is null");
            return;
        }

		logger.LogInformation("Receive stop message for {HostName}/{ServcieName}/{Origin}", message.HostName, message.ServiceName, message.Origin);

        if (string.IsNullOrWhiteSpace(message.HostName))
        {
            logger.LogWarning("HostName is null or empty");
            return;
        }

        if (string.IsNullOrWhiteSpace(message.ServiceName))
        {
            logger.LogWarning("ServiceName is null or empty");
			return;
        }

		if (message.Timeout < DateTime.Now)
        {
            logger.LogInformation("stop message is too old");
            return;
        }

        if (!settings.ServiceName.Equals(message.ServiceName, StringComparison.InvariantCultureIgnoreCase)
            || !settings.HostName.Equals(message.HostName, StringComparison.InvariantCultureIgnoreCase))
        {
            // Not for me
            logger.LogInformation("Message stop is not for me svc {S1} <-> {S2} host {H1} <-> {H2}", 
                message.ServiceName, 
                settings.ServiceName, 
                message.HostName, 
                settings.HostName);
            return;
        }

        logger.LogInformation("Try to close the service {ServiceName} on {HostName}", message.ServiceName, message.HostName);

        await bus.EnqueueMessage(settings.StopServiceReportQueueName, new StopServiceReport
        {
            ActionSourceId = message.ActionId,
            ServiceName = settings.ServiceName,
            HostName = settings.MachineName,
            State = Shared.ServiceState.TryToStop,
            Origin = message.Origin
        },cancellationToken: cancellationToken);

        try
        {
            hostApplicationLifetime.StopApplication();
			logger.LogInformation("Service {ServiceName} on {HostName} is stopping", message.ServiceName, message.HostName);
		}
		catch (Exception ex)
        {
            logger.LogError(ex, "Error when try to stop the service {ServiceName} on {HostName}", message.ServiceName, message.HostName);
		}

        _timer = new System.Timers.Timer();
        _timer.Interval = 10 * 1000;
        _timer.Elapsed += ExitTimerElapsed;
        _timer.Start();
	}

	private async void ExitTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
		_timer.Stop();
		_timer.Elapsed -= ExitTimerElapsed;
        _timer.Dispose();

		logger.LogWarning("Try to close the service {ServiceName} fail with soft method", settings.ServiceName);

		await bus.EnqueueMessage(settings.StopServiceReportQueueName, new StopServiceReport
        {
            ServiceName = settings.ServiceName,
            HostName = settings.MachineName,
            State = Shared.ServiceState.ForceInnerKill
        });

        Environment.Exit(0);
    }
}