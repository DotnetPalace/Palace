using System.Reflection;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Palace.Shared;
using Palace.Shared.Messages;

namespace Palace.Client;

public class MainWorker(
    ILogger<MainWorker> logger,
    PalaceSettings settings,
    ArianeBus.IServiceBus bus
    )
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var entryAssembly = Assembly.GetEntryAssembly()!;
        var version = $"{entryAssembly.GetName().Version}";
        var fileInfo = new System.IO.FileInfo(entryAssembly.Location);
        var startedDate = DateTime.Now;

        while (!stoppingToken.IsCancellationRequested)
        {
            var rmi = new RunningMicroserviceInfo
            {
                ServiceName = settings.ServiceName,
                HostName = settings.HostName,
                Version = version,
                Location = fileInfo.FullName,
                UserInteractive = System.Environment.UserInteractive,
                LastWriteTime = fileInfo.LastWriteTime,
                ThreadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count,
                ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
                StartedDate = startedDate,
                CommandLine = System.Environment.CommandLine,
                EnvironmentName = settings.HostEnvironmentName,
                ServiceState = ServiceState.Running,
                WorkingSet = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64,
            };

            await bus.EnqueueMessage(settings.ServiceHealthQueueName, new Shared.Messages.ServiceHealthCheck
            {
                HostName = settings.HostName,
                ServiceInfo = rmi
            }, cancellationToken: stoppingToken);

            logger.LogTrace("Worker running at: {Time}", DateTimeOffset.Now);
            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(settings.ScanIntervalInSeconds * 1000, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Service stopping");

        // Send a message to the bus to say that the service is stopping
        await bus.EnqueueMessage(settings.StopServiceReportQueueName, new StopServiceReport
        {
            ServiceName = settings.ServiceName,
            HostName = settings.HostName,
            State = ServiceState.Offline
        }, cancellationToken: cancellationToken);

        await base.StopAsync(cancellationToken);
    }

}
