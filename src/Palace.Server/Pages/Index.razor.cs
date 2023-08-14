using System.Threading;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Palace.Server.Models;
using Palace.Server.Pages.Shared;
using Palace.Server.Services.UpdateHandler;

using static System.Formats.Asn1.AsnWriter;

namespace Palace.Server.Pages;

public sealed partial class Index : ComponentBase, IDisposable
{
    [CascadingParameter]
    public MainLayout Parent { get; set; } = default!;

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
    List<ExtendedMicroServiceInfo> serviceInfoList = new();
    List<PackageInfo> packageInfoList = new();

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Orchestrator.OnHostChanged += Orchestrator_OnHostChanged;
            Orchestrator.OnServiceChanged += Orchestrator_OnServiceChanged;
            Orchestrator.OnPackageChanged += Orchestrator_OnPackageChanged;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var db = await DbContextFactory.CreateDbContextAsync();
        serviceSettingsList = await db.MicroServiceSettings.ToListAsync();

        hostList = Orchestrator.GetHostList().ToList();
        serviceInfoList = Orchestrator.GetServiceList().ToList();
        packageInfoList = Orchestrator.GetPackageInfoList().ToList();
    }

    void Orchestrator_OnPackageChanged(PackageInfo obj)
    {
        packageInfoList = Orchestrator.GetPackageInfoList().ToList();
        InvokeAsync(StateHasChanged);
    }

    void Orchestrator_OnServiceChanged(ExtendedMicroServiceInfo obj)
    {
        serviceInfoList = Orchestrator.GetServiceList().ToList();
        Parent.ActionTerminated();
        InvokeAsync(StateHasChanged);
    }

    void Orchestrator_OnHostChanged(Models.HostInfo hostInfo)
    {
        hostList = Orchestrator.GetHostList().ToList();
        InvokeAsync(StateHasChanged);
    }

	Models.LongAction CreateInstallServiceAction(Models.HostInfo host, Palace.Shared.MicroServiceSettings serviceSettings)
    {
        var currentUri = new Uri(NavigationManager.Uri);
        var downloadUrl = $"{currentUri.Scheme}://{currentUri.Host}:{currentUri.Port}/api/palace/download/{serviceSettings.PackageFileName}";
        var actionId = Guid.NewGuid();
        var result = new Models.LongAction
        {
            Id = actionId,
            Name = "InstallService",
            Title = "Install Service",
            Description = $"Install the service {serviceSettings.ServiceName} in host {host.HostName}",
            Action = () =>
            {
                var publish = Bus.PublishTopic(Settings.InstallServiceTopicName, new Palace.Shared.Messages.InstallService
                {
                    ActionId = actionId,
                    HostName = host.HostName,
                    ServiceSettings = serviceSettings,
                    DownloadUrl = downloadUrl
                });
                return publish;
            }
        };
        return result;
    }

	Models.LongAction CreateUnInstallServiceAction(Models.HostInfo host, Palace.Shared.MicroServiceSettings serviceSettings)
    {
		var actionId = Guid.NewGuid();
		var result = new Models.LongAction
		{
			Id = actionId,
			Name = "UnInstallService",
			Title = "UnInstall Service",
			Description = $"UnInstall the service {serviceSettings.ServiceName} in host {host.HostName}",
			Action = () =>
			{
				var pulishTask = Bus.PublishTopic(Settings.UnInstallServiceTopicName, new Palace.Shared.Messages.UnInstallService
				{
                    ActionId = actionId,
					HostName = host.HostName,
					ServiceSettings = serviceSettings
				});
				return pulishTask;
			}
		};
		return result;
    }

    Models.LongAction? CreateRecycleServiceAction(string hostName, Palace.Shared.MicroServiceSettings serviceSettings)
    {
        var serviceInfo = serviceInfoList.FirstOrDefault(s => s.ServiceName == serviceSettings.ServiceName);
        if (serviceInfo is null
            || serviceInfo.ServiceState != ServiceState.Running)
        {
            return null;
        }

        var context = new MicroserviceUpdateContext()
        {
            Id = Guid.NewGuid(),
            CurrentWorkflow = "RecycleService",
            HostName = hostName,
            ServiceSettings = serviceSettings,
            ServiceInfo = serviceInfo,
            InitialServiceState = ServiceState.Running,
            Origin = "Recycle"
        };

        var actionId = Guid.NewGuid();
        var result = new Models.LongAction
        {
            Id = context.Id,
            Name = "RecycleService",
            Title = "Recycle Service",
            Description = $"Recycle the service {serviceSettings.ServiceName} in host {hostName}",
            Action = () =>
            {
                var t = Task.Run(async () =>
                {
                    using var scope = ServiceScopeFactory.CreateScope();
                    var handlers = scope.ServiceProvider.GetServices<IUpdateHandler>();

                    var handler = handlers.Single(i => i.Name == nameof(StopServiceHandler));
                    handler.AddNextHandler(handlers.Single(i => i.Name == nameof(StartServiceHandler)));

                    var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token;
                    await handler.ProcessUpdateAsync(context, cancellationToken);
                });

                return t;
            }
        };
        return result;
    }

	Models.LongAction CreateStopAction(string hostName, string serviceName)
	{
		var actionId = Guid.NewGuid();
		var result = new Models.LongAction
		{
			Id = actionId,
			Name = "StopService",
			Title = "Stop Service",
			Description = $"Try to stop the service {serviceName} in {hostName}",
			Action = () =>
			{
				var publishTopic = Bus.PublishTopic(Settings.StopServiceTopicName, new Palace.Shared.Messages.StopService
				{
					ActionId = actionId,
					HostName = hostName,
					ServiceName = serviceName,
					Origin = "Manual"
				});
				return publishTopic;
			}
		};

		return result;
	}

	Models.LongAction CreateStopAction(Models.ExtendedMicroServiceInfo serviceInfo)
    {
        var actionId = Guid.NewGuid();
		serviceInfo.ServiceState = ServiceState.Stopping;
        StateHasChanged();
        return CreateStopAction(serviceInfo.HostName, serviceInfo.ServiceName);
    }

    Models.LongAction CreateStartAction(Palace.Shared.MicroServiceSettings serviceSettings, Models.ExtendedMicroServiceInfo serviceInfo)
    {
		var actionId = Guid.NewGuid();
		serviceInfo.ServiceState = ServiceState.Starting;
        StateHasChanged();
		var result = new Models.LongAction
		{
			Id = actionId,
			Name = "StartService",
			Title = "Start Service",
			Description = $"Try to start the service {serviceSettings.ServiceName} in {serviceInfo.HostName}",
			Action = () =>
			{
				var publishTopic = Bus.PublishTopic(Settings.StartServiceTopicName, new Palace.Shared.Messages.StartService
				{
                    ActionId = actionId,
					HostName = serviceInfo.HostName,
					ServiceSettings = serviceSettings,
				});

				return publishTopic;
			}
		};

        return result;
	}

	public void Dispose()
    {
        if (Orchestrator is not null)
        {
			Orchestrator.OnHostChanged -= Orchestrator_OnHostChanged;
			Orchestrator.OnServiceChanged -= Orchestrator_OnServiceChanged;
			Orchestrator.OnPackageChanged -= Orchestrator_OnPackageChanged;
		}
	}
}