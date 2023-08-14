using System.Diagnostics;

using ArianeBus;

using Palace.Shared;

namespace Palace.Host.MessageReaders;

public class StartService : ArianeBus.MessageReaderBase<Shared.Messages.StartService>
{
    private readonly ILogger<StartService> _logger;
    private readonly Configuration.GlobalSettings _settings;
    private readonly IServiceBus _bus;
    private ServiceState _serviceState = ServiceState.Offline;

    public StartService(ILogger<StartService> logger,
        Configuration.GlobalSettings settings,
        ArianeBus.IServiceBus bus)
    {
        _logger = logger;
        _settings = settings;
        _bus = bus;
    }

    public override async Task ProcessMessageAsync(Shared.Messages.StartService message, CancellationToken cancellationToken)
    {
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

        if (!message.HostName.Equals(_settings.HostName))
        {
            _logger.LogTrace("installation service not for me");
            return;
        }

        var mainFileName = System.IO.Path.Combine(_settings.InstallationFolder, message.ServiceSettings.ServiceName, message.ServiceSettings.MainAssembly);
        var installationFolder = System.IO.Path.Combine(_settings.InstallationFolder, message.ServiceSettings.ServiceName);
        if (!System.IO.File.Exists(mainFileName))
        {
            _logger.LogWarning("MainAssembly {mainFileName} not found in {installationFolder}", mainFileName, installationFolder);
            return;
        }

        var runningProcesses = ProcessHelper.GetRunningProcess(mainFileName);
        if (runningProcesses.Any())
        {
			_logger.LogWarning("MainAssembly {mainFileName} already running in {installationFolder}", mainFileName, installationFolder);
			return;
        }

        string startReport = null!;
        int processId = 0;
        bool isStarted = false;
        try
        {
            (startReport, processId, isStarted) = await ProcessHelper.StartMicroServiceProcess(mainFileName, message.ServiceSettings.Arguments);
            if (isStarted)
			{
				_serviceState = ServiceState.Running;
				_logger.LogInformation("Service {mainFileName} started with {processId} {report}", mainFileName, processId, startReport);
			}
			else
			{
				_serviceState = ServiceState.StartFail;
				_logger.LogError("Service {mainFileName} start failed with {report}", mainFileName, startReport);
			}
		}
        catch (Exception ex)
        {
            _serviceState = ServiceState.StartFail;
            startReport = ex.Message;
            ex.Data.Add("ServiceName", mainFileName);
            ex.Data.Add("ServiceLocation", installationFolder);
            _logger.LogError(ex, ex.Message);
        }

        await _bus.EnqueueMessage(_settings.StartingServiceReportQueueName, new Shared.Messages.StartingServiceReport
        {
            ActionSourceId = message.ActionId,
            HostName = _settings.HostName,
            InstallationFolder = installationFolder,
            ServiceName = message.ServiceSettings.ServiceName,
            ServiceState = _serviceState,
            ProcessId = processId,
            FailReason = !isStarted ? startReport : null,
            Origin = message.Origin
        });
    }
}
