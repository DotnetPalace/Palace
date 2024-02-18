using System.Runtime;

using ArianeBus;

using Palace.Host.Configuration;

namespace Palace.Host;

public class MainWorker(ILogger<MainWorker> logger,
	ArianeBus.IServiceBus bus,
	Configuration.GlobalSettings globalSettings) : BackgroundService
{
	private string _ip = null!;

	public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Service started");

		_ip = await Shared.ExternalIPResolver.GetIP();

		await base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var installedServiceList = await ProcessHelper.GetInstalledServiceList(globalSettings.InstallationFolder);
        var serviceSettingsList = installedServiceList.Select(x => x.MainAssembly).ToList();

        logger.LogInformation("{count} found already installed services", installedServiceList.Count);

        var runningServiceList = ProcessHelper.GetRunningProcess(serviceSettingsList.ToArray());
        foreach (var item in runningServiceList)
        {
            installedServiceList.RemoveAll(i => i.ServiceName is not null && i.ServiceName == i.ServiceName!);
		}

		logger.LogInformation("{count} found already running services", runningServiceList.Count);

		await PublishInstalledServices(installedServiceList, stoppingToken);

        // Lancer tous les services qui ne sont pas en �tat running
        // et marqu� dans les settings comme always started
        foreach (var item in installedServiceList.Where(i => i.AlwaysStarted))
        {
            // ProcessHelper.StartProcess(item.ServiceName, _settings.InstallationDirectory);
        }

		while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var currentDrive = System.IO.Path.GetPathRoot(System.Reflection.Assembly.GetExecutingAssembly().Location!);
                var driveInfo = new System.IO.DriveInfo(currentDrive!);
                var process = System.Diagnostics.Process.GetCurrentProcess();
                double factor = (Environment.ProcessorCount * process.TotalProcessorTime.TotalMilliseconds) * 100;
                if (factor == 0)
				{
					factor = 1;
				}
				var percentCpu = process.TotalProcessorTime.TotalMilliseconds / factor;
                await bus.EnqueueMessage(globalSettings.HostHealthCheckQueueName, new Shared.Messages.HostHealthCheck
                {
                    HostName = globalSettings.HostName,
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
                }, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
			{
				logger.LogError(ex, "Error while sending health check");
			}

            await Task.Delay(globalSettings.ScanIntervalInSeconds * 1000, stoppingToken);
            logger.LogTrace("Service up {date}", DateTime.Now);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await bus.EnqueueMessage(globalSettings.HostStoppedQueueName, new Shared.Messages.HostStopped
        {
            HostName = globalSettings.HostName,
            MachineName = System.Environment.MachineName,
        }, cancellationToken: cancellationToken);
        logger.LogInformation("Service stopped");
    }

    async Task PublishInstalledServices(List<Palace.Shared.MicroServiceSettings> serviceSettingsList, CancellationToken cancellationToken)
    { 
		foreach (var serviceSettings in serviceSettingsList)
		{
			var report = new Shared.Messages.ServiceInstallationReport
			{
				HostName = globalSettings.HostName,
				ServiceName = serviceSettings.ServiceName,
				Success = true
			};
			await bus.EnqueueMessage(globalSettings.InstallationReportQueueName, report, cancellationToken: cancellationToken);
		}
    }

}
