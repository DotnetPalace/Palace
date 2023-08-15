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

    private MicroserviceUpdateContext? _context = null;

    public StartServiceHandler(ILogger<StartServiceHandler> logger,
        ArianeBus.IServiceBus bus,
        Palace.Server.Configuration.GlobalSettings settings,
        Orchestrator orchestrator)
    {
        _logger = logger;
        _bus = bus;
        _settings = settings;
        _orchestrator = orchestrator;
    }

    public string Name => nameof(StartServiceHandler);
    public IUpdateHandler? NextHandler { get; set; }

    public async Task ProcessUpdateAsync(MicroserviceUpdateContext context, CancellationToken cancellationToken)
    {
		_orchestrator.LongActionProgress += OnProgress;

		_context = context;
        _context.ManualResetEvent = new(false);

        if (context.InitialServiceState != ServiceState.Running)
        {
            if (NextHandler is not null)
            {
				_orchestrator.LongActionProgress -= OnProgress;
				await NextHandler.ProcessUpdateAsync(context, cancellationToken);
            }
            return;
        }

        // 6 - Si le service était démarré, envoyer une demande de démarrage
        await _bus.PublishTopic(_settings.StartServiceTopicName, new Palace.Shared.Messages.StartService
        {
            ActionId = context.Id,
            HostName = context.HostName,
            ServiceSettings = context.ServiceSettings,
            OverridedArguments = context.OverridedArguments
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
            if (!_context.ManualResetEvent.SafeWaitHandle.IsClosed)
            {
                _context.ManualResetEvent.Set();
            }
        }
    }

    public void Dispose()
    {
        _orchestrator.LongActionProgress -= OnProgress;
        GC.SuppressFinalize(this);
    }

}
