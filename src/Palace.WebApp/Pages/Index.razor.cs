using Palace.Server.Models;
using Palace.Server.Services.UpdateHandler;
namespace Palace.WebApp.Pages;

public sealed partial class Index : ComponentBase, IDisposable
{
	[CascadingParameter]
	public MainLayout Parent { get; set; } = default!;

	[Inject]
	Orchestrator Orchestrator { get; set; } = default!;

	[Inject]
	ArianeBus.IServiceBus Bus { get; set; } = default!;

	[Inject]
	Palace.Server.Configuration.GlobalSettings Settings { get; set; } = default!;

	[Inject]
	NavigationManager NavigationManager { get; set; } = default!;

	[Inject]
	IServiceScopeFactory ServiceScopeFactory { get; set; } = default!;

	[Inject]
	ServiceSettingsRepository ServiceSettingsRepository { get; set; } = default!;

	[Inject]
	IPackageRepository PackageRepository { get; set; } = default!;

	[Inject]
	IPackageDownloaderService PackageDownloaderService { get; set; } = default!;


	List<MicroServiceSettings> serviceSettingsList = new();
	List<HostInfo> hostList = new();
	List<ExtendedMicroServiceInfo> serviceInfoList = new();
	List<PackageInfo> packageInfoList = new();

	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			Orchestrator.HostChanged += Orchestrator_OnHostChanged;
			Orchestrator.ServiceChanged += Orchestrator_OnServiceChanged;
			PackageRepository.PackageChanged += Orchestrator_OnPackageChanged;
		}
	}

	protected override async Task OnInitializedAsync()
	{
		await LoadLists();
	}

	async Task LoadLists()
	{
		serviceSettingsList = (await ServiceSettingsRepository.GetAll()).ToList();
		hostList = Orchestrator.GetHostList().ToList();
		serviceInfoList = Orchestrator.GetServiceList().ToList();
		packageInfoList = PackageRepository.GetPackageInfoList().ToList();
	}

	void Orchestrator_OnPackageChanged(PackageInfo obj)
	{
		packageInfoList = PackageRepository.GetPackageInfoList().ToList();
		InvokeAsync(StateHasChanged);
	}

	void Orchestrator_OnServiceChanged(ExtendedMicroServiceInfo obj)
	{
		serviceInfoList = Orchestrator.GetServiceList().ToList();
		Parent.ActionTerminated();
		InvokeAsync(StateHasChanged);
	}

	void Orchestrator_OnHostChanged(Palace.Server.Models.HostInfo hostInfo)
	{
		hostList = Orchestrator.GetHostList().ToList();
		InvokeAsync(StateHasChanged);
	}

	async Task<Palace.Server.Models.LongAction> CreateInstallServiceAction(Palace.Server.Models.HostInfo host, Palace.Shared.MicroServiceSettings serviceSettings)
	{
		await Task.Yield();
		var currentUri = new Uri(NavigationManager.Uri);
		var downloadUrl = await PackageDownloaderService.GenerateUrl(serviceSettings.PackageFileName);
		// $"{currentUri.Scheme}://{currentUri.Host}:{currentUri.Port}/api/palace/download/{serviceSettings.PackageFileName}";
		var actionId = Guid.NewGuid();
		var arguments = await ServiceSettingsRepository.GetArgumentsByHostForService(host.HostName, serviceSettings.Id);
		var result = new Palace.Server.Models.LongAction
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
					DownloadUrl = downloadUrl,
					OverridedArguments = arguments?.Arguments
				});
				return publish;
			}
		};
		return result;
	}

	async Task<Palace.Server.Models.LongAction> CreateUnInstallServiceAction(Palace.Server.Models.HostInfo host, Palace.Shared.MicroServiceSettings serviceSettings)
	{
		await Task.Yield();
		var actionId = Guid.NewGuid();
		var arguments = await ServiceSettingsRepository.GetArgumentsByHostForService(host.HostName, serviceSettings.Id);
		var result = new Palace.Server.Models.LongAction
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
					ServiceSettings = serviceSettings,
					OverrideArguments = arguments?.Arguments
				});
				return pulishTask;
			}
		};
		return result;
	}

	async Task<Palace.Server.Models.LongAction?> CreateRecycleServiceAction(string hostName, Palace.Shared.MicroServiceSettings serviceSettings)
	{
		await Task.Yield();
		var serviceInfo = serviceInfoList.FirstOrDefault(s => s.ServiceName == serviceSettings.ServiceName);
		if (serviceInfo is null
			|| serviceInfo.ServiceState != ServiceState.Running)
		{
			return null;
		}

		var arguments = await ServiceSettingsRepository.GetArgumentsByHostForService(hostName, serviceSettings.Id);
		var context = new MicroserviceUpdateContext()
		{
			Id = Guid.NewGuid(),
			CurrentStep = "RecycleService",
			HostName = hostName,
			ServiceSettings = serviceSettings,
			ServiceInfo = serviceInfo,
			InitialServiceState = ServiceState.Running,
			Origin = "Recycle",
			OverridedArguments = arguments?.Arguments
		};

		var actionId = Guid.NewGuid();
		var result = new Palace.Server.Models.LongAction
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

	async Task<Palace.Server.Models.LongAction> CreateStopAction(string hostName, string serviceName)
	{
		await Task.Yield();
		var actionId = Guid.NewGuid();
		var result = new Palace.Server.Models.LongAction
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

	async Task<Palace.Server.Models.LongAction> CreateStopAction(Palace.Server.Models.ExtendedMicroServiceInfo serviceInfo)
	{
		await Task.Yield();
		var actionId = Guid.NewGuid();
		serviceInfo.ServiceState = ServiceState.Stopping;
		StateHasChanged();
		return await CreateStopAction(serviceInfo.HostName, serviceInfo.ServiceName);
	}

	async Task<Palace.Server.Models.LongAction> CreateStartAction(Palace.Shared.MicroServiceSettings serviceSettings, Palace.Server.Models.ExtendedMicroServiceInfo serviceInfo)
	{
		var actionId = Guid.NewGuid();
		serviceInfo.ServiceState = ServiceState.Starting;
		StateHasChanged();
		var arguments = await ServiceSettingsRepository.GetArgumentsByHostForService(serviceInfo.HostName, serviceSettings.Id);
		var result = new Palace.Server.Models.LongAction
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
					OverridedArguments = arguments is null ? null : arguments.Arguments
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
			Orchestrator.HostChanged -= Orchestrator_OnHostChanged;
			Orchestrator.ServiceChanged -= Orchestrator_OnServiceChanged;
			PackageRepository.PackageChanged -= Orchestrator_OnPackageChanged;
		}
	}
}