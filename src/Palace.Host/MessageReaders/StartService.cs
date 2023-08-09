using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

using ArianeBus;

using Microsoft.Extensions.Logging;

using Palace.Host.Configuration;
using Palace.Shared;

namespace Palace.Host.MessageReaders;

public class StartService : ArianeBus.MessageReaderBase<Shared.Messages.StartService>
{
    private readonly ILogger<StartService> _logger;
    private readonly Configuration.GlobalSettings _settings;
    private readonly IServiceBus _bus;
    private ServiceState _serviceState = ServiceState.Offline;
    private int _processId = 0;
    private string? _errorMessage = null;

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
        _processId = 0;
        _errorMessage = null;

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

        try
        {
            StartMicroService(message.ServiceSettings, mainFileName);
        }
        catch (Exception ex)
        {
            _serviceState = ServiceState.StartFail;
            _errorMessage = ex.Message;
            ex.Data.Add("ServiceName", mainFileName);
            ex.Data.Add("ServiceLocation", installationFolder);
            _logger.LogError(ex, ex.Message);
        }

        await _bus.EnqueueMessage(_settings.StartingServiceReportQueueName, new Shared.Messages.StartingServiceReport
        {
            HostName = _settings.HostName,
            InstallationFolder = installationFolder,
            ServiceName = message.ServiceSettings.ServiceName,
            ServiceState = _serviceState,
            ProcessId = _processId,
            FailReason = _errorMessage
        });
    }

    public void StartMicroService(Shared.MicroServiceSettings serviceSettings, string mainFileName)
    {
        _logger.LogInformation("Try to start {mainFileName}", mainFileName);
        var psi = new ProcessStartInfo("dotnet");

        psi.Arguments = $"{mainFileName} {serviceSettings.Arguments}".Trim();

        psi.CreateNoWindow = false;
        psi.UseShellExecute = false;
        psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        psi.RedirectStandardError = true;
        psi.ErrorDialog = false;

        var process = new Process();
        process.StartInfo = psi;
        process.EnableRaisingEvents = true;
        process.ErrorDataReceived += (s, arg) =>
        {
            if (string.IsNullOrWhiteSpace(arg.Data))
            {
                return;
            }
            _logger.LogCritical(arg.Data);
            _serviceState = Shared.ServiceState.StartFail;
            // TODO: Send Error message
        };

        var start = process.Start();
        if (!start)
        {
            _serviceState = Shared.ServiceState.StartFail;
        }
        else
        {
            _processId = process.Id;
            _serviceState = Shared.ServiceState.Starting;
        }
        process.BeginErrorReadLine();
    }

    
}
