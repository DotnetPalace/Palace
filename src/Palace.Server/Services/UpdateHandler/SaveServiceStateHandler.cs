using System.Diagnostics;

using Palace.Server.Models;

namespace Palace.Server.Services.UpdateHandler;

public class SaveServiceStateHandler : IUpdateHandler
{
    private readonly Orchestrator _orchestrator;

    public SaveServiceStateHandler(Orchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public string Name => nameof(SaveServiceStateHandler);

    public IUpdateHandler? NextHandler { get; set; }

    public async Task ProcessUpdateAsync(MicroserviceUpdateContext context, CancellationToken cancellationToken)
    {
        context.InitialServiceState = context.ServiceInfo.ServiceState;
        context.ServiceInfo.ServiceState = ServiceState.UpdateDetected;
        _orchestrator.AddOrUpdateMicroServiceInfo(context.ServiceInfo);

        if (NextHandler is not null)
        {
            await NextHandler!.ProcessUpdateAsync(context, cancellationToken);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
