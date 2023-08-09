using System.Diagnostics;
using System.Runtime;
using System.Security.Cryptography.Xml;
using System.Threading;

using ArianeBus;

using Palace.Server.Models;

namespace Palace.Server.Services.UpdateHandler;

public class StartServiceHandler : IUpdateHandler
{
    private readonly ILogger<StartServiceHandler> _logger;
    private readonly IServiceBus _bus;
    private readonly Configuration.GlobalSettings _settings;
    private readonly Orchestrator _orchestrator;

    private MicroserviceUpdateContext? _processMicroserviceUpdate = null;

    public StartServiceHandler(ILogger<StartServiceHandler> logger,
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

    public string Name => nameof(StartServiceHandler);
    public IUpdateHandler? NextHandler { get; set; }

    public async Task ProcessUpdateAsync(MicroserviceUpdateContext context, CancellationToken cancellationToken)
    {
        _processMicroserviceUpdate = context;
        _processMicroserviceUpdate.ManualResetEvent = new(false);

        if (context.InitialServiceState != ServiceState.Running)
        {
            if (NextHandler is not null)
            {
                await NextHandler.ProcessUpdateAsync(context, cancellationToken);
            }
            return;
        }

        // 6 - Si le service était démarré, envoyer une demande de démarrage
        await _bus.PublishTopic(_settings.StartServiceTopicName, new Palace.Shared.Messages.StartService
        {
            HostName = context.HostName,
            ServiceSettings = context.ServiceSettings,
        });

        // 7 - Attendre le retour du service démarré
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
            if (loop > 2)
            {
                _logger.LogWarning("Update aborted for service {serviceName} for host {host}", context.ServiceInfo.ServiceName, context.HostName);
                return;
            }
        }

        if (NextHandler is not null)
        {
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
            && msi.ServiceState == ServiceState.Running)
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
