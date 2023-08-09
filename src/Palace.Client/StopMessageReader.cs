using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ArianeBus;

using Palace.Shared.Messages;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Palace.Client;

public class StopMessageReader : MessageReaderBase<StopService>
{
    private readonly PalaceSettings _settings;
    private readonly ILogger<StopMessageReader> _logger;
    private readonly IServiceBus _bus;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private System.Timers.Timer _timer = default!;

    public StopMessageReader(PalaceSettings settings,
        ILogger<StopMessageReader> logger,
        IServiceBus bus,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _settings = settings;
        _logger = logger;
        _bus = bus;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public override async Task ProcessMessageAsync(StopService message, CancellationToken cancellationToken)
    {
        if (message is null)
        {
            _logger.LogWarning("message is null");
            return;
        }

        if (message.Timeout < DateTime.Now)
        {
            _logger.LogTrace("message is too old");
            return;
        }

        if (message.ServiceName != _settings.ServiceName
            || message.HostName != _settings.HostName)
        {
            // Not for me
            return;
        }

        _timer = new System.Timers.Timer();
        _timer.Interval = _settings.TimeoutInSecondBeforeKillService * 1000;
        _timer.Elapsed += ExitTimerElapsed;

        _logger.LogInformation($"Try to close the service {_settings.ServiceName}");

        await _bus.EnqueueMessage(_settings.StopServiceReportQueueName, new StopServiceReport
        {
            ServiceName = _settings.ServiceName,
            HostName = _settings.MachineName,
            State = Shared.ServiceState.TryToStop
        });

        _hostApplicationLifetime.StopApplication();
        _timer.Start();
    }

    private async void ExitTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        _logger.LogWarning($"Try to close the service {_settings.ServiceName} fail with soft method");
        _timer.Stop();

        await _bus.EnqueueMessage(_settings.StopServiceReportQueueName, new StopServiceReport
        {
            ServiceName = _settings.ServiceName,
            HostName = _settings.MachineName,
            State = Shared.ServiceState.ForceInnerKill
        });

        Environment.Exit(0);
    }
}