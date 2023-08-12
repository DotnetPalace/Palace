using System.Runtime;

using ArianeBus;

using Palace.Host.Configuration;

namespace Palace.Host;

public class MainWorker : BackgroundService
{
    private readonly ILogger<MainWorker> _logger;
    private readonly IServiceBus _bus;
    private readonly GlobalSettings _settings;
    private string _ip = null!;

    public MainWorker(ILogger<MainWorker> logger,
        ArianeBus.IServiceBus bus,
        Configuration.GlobalSettings globalSettings)
    {
        _logger = logger;
        _bus = bus;
        _settings = globalSettings;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Service started");
		await base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
		_ip = await Shared.ExternalIPResolver.GetIP();

        var installedServiceList = await ProcessHelper.GetInstalledServiceList(_settings.InstallationFolder);
        var serviceSettingsList = installedServiceList.Select(x => x.ServiceName).ToList();

        _logger.LogInformation("{count} found already installed services", installedServiceList.Count);

        var runningServiceList = ProcessHelper.GetRunningProcess(serviceSettingsList.ToArray());
        foreach (var item in runningServiceList)
        {
            installedServiceList.RemoveAll(i => i.ServiceName == i.ServiceName);
		}

		_logger.LogInformation("{count} found already running services", runningServiceList.Count);

		await PublishInstalledServices(installedServiceList);

        // Lancer tous les services qui ne sont pas en état running
        // et marqué dans les settings comme always started
        foreach (var item in installedServiceList.Where(i => i.AlwaysStarted))
        {
            // ProcessHelper.StartProcess(item.ServiceName, _settings.InstallationDirectory);
        }

		while (!stoppingToken.IsCancellationRequested)
        {
            var currentDrive = System.IO.Path.GetPathRoot(System.Reflection.Assembly.GetExecutingAssembly().Location!);
            var driveInfo = new System.IO.DriveInfo(currentDrive!);
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var percentCpu = process.TotalProcessorTime.TotalMilliseconds / (Environment.ProcessorCount * process.TotalProcessorTime.TotalMilliseconds) * 100;
            await _bus.EnqueueMessage(_settings.HostHealthCheckQueueName, new Shared.Messages.HostHealthCheck
            {
                HostName = _settings.HostName,
                MachineName = System.Environment.MachineName,
                ExternalIp = _ip,
                MainFileName = System.Reflection.Assembly.GetExecutingAssembly().Location!,
                Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!.ToString(),
                TotalDriveSize = driveInfo.TotalSize,
                TotalFreeSpaceOfDriveSize = driveInfo.TotalFreeSpace,
                OsDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                OsVersion = System.Environment.OSVersion.ToString(),
                ProcessId = process.Id,
                PercentCpu = percentCpu
            });

            await Task.Delay(_settings.ScanIntervalInSeconds * 1000, stoppingToken);
            _logger.LogTrace("Service up {date}", DateTime.Now);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _bus.EnqueueMessage(_settings.HostStoppedQueueName, new Shared.Messages.HostStopped
        {
            HostName = _settings.HostName,
            MachineName = System.Environment.MachineName,
        });
        _logger.LogInformation("Service stopped");
    }

    async Task PublishInstalledServices(List<Palace.Shared.MicroServiceSettings> serviceSettingsList)
    { 
		foreach (var serviceSettings in serviceSettingsList)
		{
			var report = new Shared.Messages.ServiceInstallationReport
			{
				HostName = _settings.HostName,
				ServiceName = serviceSettings.ServiceName,
				Success = true
			};
			await _bus.EnqueueMessage(_settings.InstallationReportQueueName, report);
		}
    }

}
