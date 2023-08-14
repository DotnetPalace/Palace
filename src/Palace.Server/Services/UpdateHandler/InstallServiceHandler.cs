using System.Diagnostics;
using System.Runtime;
using System.Security.Cryptography.Xml;
using System.Threading;

using ArianeBus;

using Palace.Server.Models;

namespace Palace.Server.Services.UpdateHandler;

public class InstallServiceHandler : IUpdateHandler
{
    private readonly ILogger<InstallServiceHandler> _logger;
    private readonly IServiceBus _bus;
    private readonly Configuration.GlobalSettings _settings;
    private readonly Orchestrator _orchestrator;

    private MicroserviceUpdateContext? _processMicroserviceUpdate = null;

    public InstallServiceHandler(ILogger<InstallServiceHandler> logger,
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

    public string Name => nameof(InstallServiceHandler);
    public IUpdateHandler? NextHandler { get; set; }

    public async Task ProcessUpdateAsync(MicroserviceUpdateContext context, CancellationToken cancellationToken)
    {
        _processMicroserviceUpdate = context;
        context.ManualResetEvent = new(false);

        context.ServiceInfo.Log = "Try to install";
        _orchestrator.AddOrUpdateMicroServiceInfo(context.ServiceInfo);

        // 4 - Envoyer une demande d'installation
        _logger.LogInformation("Send install service {serviceName} for host {host}", context.ServiceInfo.ServiceName, context.HostName);
        var downloadUrl = $"{_settings.CurrentUrl}/api/palace/download/{context.ServiceSettings.PackageFileName}";
        await _bus.PublishTopic(_settings.InstallServiceTopicName, new Palace.Shared.Messages.InstallService
        {
            ActionId = Guid.NewGuid(),
            HostName = context.HostName,
            ServiceSettings = context.ServiceSettings,
            DownloadUrl = downloadUrl,
            Trigger = "FromUpdate"
        });

        // 5 - Attendre le retour d'installation réussie
        _logger.LogInformation("Wait for service {serviceName} installed for host {host}", context.ServiceInfo.ServiceName, context.HostName);
        var loop = 0;
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var signalReceived = context.ManualResetEvent.WaitOne(60 * 1000);
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
            && msi.ServiceState == ServiceState.Updated)
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
