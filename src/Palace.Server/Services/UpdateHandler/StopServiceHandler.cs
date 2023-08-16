using System;
using System.Diagnostics;
using System.Runtime;
using System.Threading;

using ArianeBus;

using Palace.Server.Models;

namespace Palace.Server.Services.UpdateHandler;

public sealed class StopServiceHandler : IUpdateHandler
{
    private readonly ILogger<StopServiceHandler> _logger;
    private readonly IServiceBus _bus;
    private readonly Configuration.GlobalSettings _settings;
    private readonly Orchestrator _orchestrator;

    private MicroserviceUpdateContext? _context = null;

    public StopServiceHandler(ILogger<StopServiceHandler> logger,
        ArianeBus.IServiceBus bus,
        Palace.Server.Configuration.GlobalSettings settings, 
        Orchestrator orchestrator)
    {
        _logger = logger;
        _bus = bus;
        _settings = settings;
        _orchestrator = orchestrator;
    }

    public string Name => nameof(StopServiceHandler);
    public IUpdateHandler? NextHandler { get; set; }

    public async Task ProcessUpdateAsync(MicroserviceUpdateContext context, CancellationToken cancellationToken)
    {
		_orchestrator.LongActionProgress += OnProgress;
		_context = context;
        context.ManualResetEvent = new(false);

        // 2 - Envoyer une demande de stop
        _logger.LogInformation("Send stop service {serviceName} for host {host}", context.ServiceInfo.ServiceName, context.HostName);
        await _bus.PublishTopic(_settings.StopServiceTopicName, new Palace.Shared.Messages.StopService
        {
            ActionId = context.Id,
            HostName = context.HostName,
            ServiceName = context.ServiceSettings.ServiceName,
            Origin = context.Origin,
            Timeout = DateTime.Now.AddSeconds(3),
		});

        // 3 - Attendre le retour offline
        _logger.LogInformation("Wait for service {serviceName} offline for host {host}", context.ServiceInfo.ServiceName, context.HostName);

        var loop = 0;
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var signalReceived = context.ManualResetEvent.WaitOne(15 * 1000);

            if (signalReceived)
            {
                break;
            }
			context.ManualResetEvent.Reset();

			loop++;
            context.ServiceInfo.Log = $"Try to stop service {loop}/2";
            _orchestrator.AddOrUpdateMicroServiceInfo(context.ServiceInfo);
            if (loop > 2)
            {
                _logger.LogWarning("service {serviceName} is probably stopped for host {host}", context.ServiceInfo.ServiceName, context.HostName);
                break;
            }
        }

        if (NextHandler is not null)
        {
            _orchestrator.LongActionProgress -= OnProgress;
            await NextHandler.ProcessUpdateAsync(context, cancellationToken);
        }
    }

    private void OnProgress(Models.ActionResult actionResult)
    {
        if (_context is null)
        {
            return;
        }

        if (actionResult.ActionId == _context.Id)
        {
            _context.CurrentStep = $"{actionResult.StepName}";
            _context.ManualResetEvent.Set();
        }
    }

    public void Dispose()
    {
        _orchestrator.LongActionProgress -= OnProgress;
        GC.SuppressFinalize(this);
    }
}
