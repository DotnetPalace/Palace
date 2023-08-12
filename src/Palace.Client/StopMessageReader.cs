using ArianeBus;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Palace.Shared.Messages;

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

		_logger.LogTrace("Receive stop message for {hostName}/{servcieName}", message.ServiceName, message.HostName);

        if (string.IsNullOrWhiteSpace(message.HostName))
        {
            _logger.LogWarning("HostName is null or empty");
            return;
        }

        if (string.IsNullOrWhiteSpace(message.ServiceName))
        {
            _logger.LogWarning("ServiceName is null or empty");
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
            _logger.LogTrace("Message stop is not for me svc {s1} <-> {s2} host {h1} <-> {h2}", 
                message.ServiceName, 
                _settings.ServiceName, 
                message.HostName, 
                _settings.HostName);
            return;
        }

        _timer = new System.Timers.Timer();
        _timer.Interval = _settings.TimeoutInSecondBeforeKillService * 1000;
        _timer.Elapsed += ExitTimerElapsed;

        _logger.LogInformation($"Try to close the service {message.ServiceName} on {message.HostName}");

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