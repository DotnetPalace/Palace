using System.Diagnostics;
using System.Runtime;
using System.Security.Cryptography.Xml;
using System.Threading;

using ArianeBus;

using Microsoft.AspNetCore.Mvc.Infrastructure;

using Palace.Server.Models;
using Palace.Shared;

namespace Palace.Server.Services.UpdateHandler;

public class InstallServiceHandler : IUpdateHandler
{
    private readonly ILogger<InstallServiceHandler> _logger;
    private readonly IServiceBus _bus;
    private readonly Configuration.GlobalSettings _settings;
    private readonly Orchestrator _orchestrator;
    private readonly IPackageDownloaderService _packageDownloaderService;
    private MicroserviceUpdateContext? _context = null;

    public InstallServiceHandler(ILogger<InstallServiceHandler> logger,
        ArianeBus.IServiceBus bus,
        Palace.Server.Configuration.GlobalSettings settings,
        Orchestrator orchestrator,
        IPackageDownloaderService packageDownloaderService)
    {
        _logger = logger;
        _bus = bus;
        _settings = settings;
        _orchestrator = orchestrator;
        _packageDownloaderService = packageDownloaderService;
    }

    public string Name => nameof(InstallServiceHandler);
    public IUpdateHandler? NextHandler { get; set; }

    public async Task ProcessUpdateAsync(MicroserviceUpdateContext context, CancellationToken cancellationToken)
    {
		_orchestrator.LongActionProgress += OnProgress;

		_context = context;
        context.ManualResetEvent = new(false);

        context.ServiceInfo.Log = "Try to install";
        _orchestrator.AddOrUpdateMicroServiceInfo(context.ServiceInfo);

        // 4 - Envoyer une demande d'installation
        _logger.LogInformation("Send install service {serviceName} for host {host}", context.ServiceInfo.ServiceName, context.HostName);
        var downloadUrl = await _packageDownloaderService.GenerateUrl(context.ServiceSettings.PackageFileName);
        // var downloadUrl = $"{_settings.CurrentUrl}/api/palace/download/{context.ServiceSettings.PackageFileName}";
        await _bus.PublishTopic(_settings.InstallServiceTopicName, new Palace.Shared.Messages.InstallService
        {
            ActionId = context.Id,
            HostName = context.HostName,
            ServiceSettings = context.ServiceSettings,
            DownloadUrl = downloadUrl,
            Trigger = "FromUpdate",
            OverridedArguments = context.OverridedArguments
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
