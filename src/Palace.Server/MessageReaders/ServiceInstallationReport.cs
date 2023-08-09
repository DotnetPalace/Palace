using Palace.Server.Services;

namespace Palace.Server.MessageReaders;

public class ServiceInstallationReport : ArianeBus.MessageReaderBase<Palace.Shared.Messages.ServiceInstallationReport>
{
    private readonly Orchestrator _orchestrator;
    private readonly ILogger<ServiceInstallationReport> _logger;

    public ServiceInstallationReport(Services.Orchestrator orchestrator,
        ILogger<ServiceInstallationReport> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public override async Task ProcessMessageAsync(Shared.Messages.ServiceInstallationReport message, CancellationToken cancellationToken)
    {
        await Task.Yield();

        if (message is null)
        {
            _logger.LogError("message is null");
            return;
        }

        if (message.Timeout < DateTime.Now)
        {
            _logger.LogTrace("message is too old");
            return;
        }

        var serviceInfo = new Models.ExtendedMicroServiceInfo();
        serviceInfo.HostName = message.HostName;
        serviceInfo.ServiceName = message.ServiceName;
        serviceInfo.Location = message.InstallationFolder;
        if (!message.Success)
        {
            serviceInfo.ServiceState = ServiceState.InstallationFailed;
            serviceInfo.FailReason = message.FailReason;
        }
        else
        {
            if (message.Trigger == "FromUpdate")
            {
                serviceInfo.ServiceState = ServiceState.Updated;
            }
            else
            {
                serviceInfo.ServiceState = ServiceState.Offline;
            }
        }

        _orchestrator.AddOrUpdateMicroServiceInfo(serviceInfo);
    }

}

