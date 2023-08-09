using System.Threading;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Palace.Server.Models;
using Palace.Server.Services.UpdateHandler;

using static System.Formats.Asn1.AsnWriter;

namespace Palace.Server.Pages;

public partial class Index : ComponentBase
{
    [Inject]
    Services.Orchestrator Orchestrator { get; set; } = default!;

    [Inject]
    IDbContextFactory<Services.PalaceDbContext> DbContextFactory { get; set; } = default!;

    [Inject]
    ArianeBus.IServiceBus Bus { get; set; } = default!;

    [Inject]
    Configuration.GlobalSettings Settings { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    IServiceScopeFactory ServiceScopeFactory { get; set; } = default!;

    List<MicroServiceSettings> serviceSettingsList = new();
    List<HostInfo> hostList = new();
    List<ExtendedMicroServiceInfo> runningServiceList = new();
    List<PackageInfo> packageInfoList = new();

    protected override async Task OnInitializedAsync()
    {
        await Task.Yield();

        var db = await DbContextFactory.CreateDbContextAsync();
        serviceSettingsList = await db.MicroServiceSettings.ToListAsync();

        hostList = Orchestrator.GetHostList();
        runningServiceList = Orchestrator.GetServiceList();
        packageInfoList = Orchestrator.GetPackageInfoList();

		Orchestrator.OnHostChanged += Orchestrator_OnHostChanged;
        Orchestrator.OnServiceChanged += Orchestrator_OnServiceChanged;
		Orchestrator.OnPackageChanged += Orchestrator_OnPackageChanged;
    }

	private void Orchestrator_OnPackageChanged(PackageInfo obj)
	{
		packageInfoList = Orchestrator.GetPackageInfoList();
		InvokeAsync(StateHasChanged);
	}

	private void Orchestrator_OnServiceChanged(ExtendedMicroServiceInfo obj)
    {
        runningServiceList = Orchestrator.GetServiceList();
        InvokeAsync(StateHasChanged);
    }

    void Orchestrator_OnHostChanged(Models.HostInfo hostInfo)
	{
		hostList = Orchestrator.GetHostList();
        InvokeAsync(StateHasChanged);
	}

    async Task InstallService(Models.HostInfo host, Palace.Shared.MicroServiceSettings serviceSettings)
	{
        var currentUri = new Uri(NavigationManager.Uri);
        var downloadUrl = $"{currentUri.Scheme}://{currentUri.Host}:{currentUri.Port}/api/palace/download/{serviceSettings.PackageFileName}";
        await Bus.PublishTopic(Settings.InstallServiceTopicName, new Palace.Shared.Messages.InstallService
        {
            HostName = host.HostName,
            ServiceSettings = serviceSettings,
            DownloadUrl = downloadUrl
        });
	}

	async Task StartService(Models.HostInfo host, Palace.Shared.MicroServiceSettings serviceSettings)
	{
		await Bus.PublishTopic(Settings.StartServiceTopicName, new Palace.Shared.Messages.StartService
		{
			HostName = host.HostName,
			ServiceSettings = serviceSettings,
		});
	}

	async Task StopService(string hostName, Palace.Shared.MicroServiceSettings serviceSettings)
	{
		await Bus.PublishTopic(Settings.StopServiceTopicName, new Palace.Shared.Messages.StopService
		{
			HostName = hostName,
			ServiceName = serviceSettings.ServiceName,
		});
	}

	void RecycleService(string hostName, Palace.Shared.MicroServiceSettings serviceSettings)
	{
        var serviceInfo = runningServiceList.FirstOrDefault(s => s.ServiceName == serviceSettings.ServiceName);
        if (serviceInfo is null
            || serviceInfo.ServiceState != ServiceState.Running)
        {
            return;
        }
        var context = new MicroserviceUpdateContext()
        {
            Id = Guid.NewGuid(),
            CurrentWorkflow = "RecycleService",
            HostName = hostName,
            ServiceSettings = serviceSettings,
            ServiceInfo = serviceInfo,
            InitialServiceState = ServiceState.Running
        };

        Task.Run(async () =>
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<IUpdateHandler>();

            var handler = handlers.Single(i => i.Name == nameof(StopServiceHandler));
            handler.AddNextHandler(handlers.Single(i => i.Name == nameof(StartServiceHandler)));

            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token;
            await handler.ProcessUpdateAsync(context, cancellationToken);
        });
    }
}