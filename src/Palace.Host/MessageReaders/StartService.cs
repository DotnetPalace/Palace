using Palace.Shared;

namespace Palace.Host.MessageReaders;

public class StartService(
    ILogger<StartService> logger,
    Configuration.GlobalSettings settings,
    ArianeBus.IServiceBus bus,
    IProcessHelper processHelper
    )
    : ArianeBus.MessageReaderBase<Shared.Messages.StartService>
{
    private ServiceState _serviceState = ServiceState.Offline;

    public override async Task ProcessMessageAsync(Shared.Messages.StartService message, CancellationToken cancellationToken)
    {
        if (message is null)
        {
            logger.LogError("message is null");
            return;
        }

        if (message.Timeout < DateTime.Now)
        {
            logger.LogTrace("message is too old");
            return;
        }

        if (!message.HostName.Equals(settings.HostName))
        {
            logger.LogTrace("installation service not for me");
            return;
        }

        var mainFileName = System.IO.Path.Combine(settings.InstallationFolder, message.ServiceSettings.ServiceName, message.ServiceSettings.MainAssembly);
        var installationFolder = System.IO.Path.Combine(settings.InstallationFolder, message.ServiceSettings.ServiceName);
        if (!System.IO.File.Exists(mainFileName))
        {
            logger.LogWarning("MainAssembly {MainFileName} not found in {InstallationFolder}", mainFileName, installationFolder);
            return;
        }

        var commandLine = $"{mainFileName} {message.OverridedArguments ?? message.ServiceSettings.Arguments}".Trim();

        var runningProcesses = processHelper.GetRunningProcess(commandLine);
        if (runningProcesses.Any())
        {
            logger.LogWarning("MainAssembly {MainFileName} already running in {InstallationFolder}", mainFileName, installationFolder);
            return;
        }

        string startReport = null!;
        int processId = 0;
        bool isStarted = false;
        try
        {
            (startReport, processId, isStarted) = await processHelper.StartMicroServiceProcess(commandLine, cancellationToken);
            if (isStarted)
            {
                _serviceState = ServiceState.Running;
                logger.LogInformation("Service {MainFileName} started with {ProcessId} {Report}", mainFileName, processId, startReport);
            }
            else
            {
                _serviceState = ServiceState.StartFail;
                logger.LogError("Service {MainFileName} start failed with {Report}", mainFileName, startReport);
            }
        }
        catch (Exception ex)
        {
            _serviceState = ServiceState.StartFail;
            startReport = ex.Message;
            ex.Data.Add("ServiceName", mainFileName);
            ex.Data.Add("ServiceLocation", installationFolder);
            logger.LogError(ex, ex.Message);
        }

        await bus.EnqueueMessage(settings.StartingServiceReportQueueName, new Shared.Messages.StartingServiceReport
        {
            ActionSourceId = message.ActionId,
            HostName = settings.HostName,
            InstallationFolder = installationFolder,
            ServiceName = message.ServiceSettings.ServiceName,
            ServiceState = _serviceState,
            ProcessId = processId,
            FailReason = !isStarted ? startReport : null,
            Origin = message.Origin
        });
    }
}
