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

    private MicroserviceUpdateContext? _processMicroserviceUpdate = null;

    public StopServiceHandler(ILogger<StopServiceHandler> logger,
        ArianeBus.IServiceBus bus,
        Palace.Server.Configuration.GlobalSettings settings, 
        Orchestrator orchestrator)
    {
        _logger = logger;
        _bus = bus;
        _settings = settings;
        _orchestrator = orchestrator;
        _orchestrator.OnServiceChanged += OnServiceChanged;
    }

    public string Name => nameof(StopServiceHandler);
    public IUpdateHandler? NextHandler { get; set; }

    public async Task ProcessUpdateAsync(MicroserviceUpdateContext context, CancellationToken cancellationToken)
    {
        _processMicroserviceUpdate = context;

        // 2 - Envoyer une demande de stop
        _logger.LogInformation("Send stop service {serviceName} for host {host}", context.ServiceInfo.ServiceName, context.HostName);
        await _bus.PublishTopic(_settings.StopServiceTopicName, new Palace.Shared.Messages.StopService
        {
            ActionId = context.Id,
            HostName = context.HostName,
            ServiceName = context.ServiceSettings.ServiceName,
            Origin = context.Origin,
            Timeout = DateTime.Now.AddSeconds(3)
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

            var signalReceived = context.ManualResetEvent.WaitOne(30 * 1000);
            context.ManualResetEvent.Reset();

            if (signalReceived)
            {
                break;
            }

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
            _orchestrator.OnServiceChanged -= OnServiceChanged;
            _orchestrator.AddOrUpdateMicroServiceInfo(context.ServiceInfo);
            await NextHandler.ProcessUpdateAsync(context, cancellationToken);
        }
    }

    private void OnServiceChanged(ExtendedMicroServiceInfo msi)
    {
        if (_processMicroserviceUpdate is null)
        {
            return;
        }

        if (msi.Key == _processMicroserviceUpdate.Key
            && msi.ServiceState == ServiceState.Offline)
        {
            _logger.LogInformation("Service {serviceName} is {serviceState} for host {host}", _processMicroserviceUpdate.ServiceInfo.ServiceName, msi.ServiceState, _processMicroserviceUpdate.HostName);
            _processMicroserviceUpdate.CurrentWorkflow = $"{msi.ServiceState}";
            _processMicroserviceUpdate.ServiceInfo.ServiceState = msi.ServiceState;
            _processMicroserviceUpdate.ManualResetEvent.Set();
        }
    }

    public void Dispose()
    {
        _orchestrator.OnServiceChanged -= OnServiceChanged;
        GC.SuppressFinalize(this);
    }
}
