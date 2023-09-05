namespace Palace.WebApp.Pages;

public partial class EditMicroServiceSettings : ComponentBase
{
	[CascadingParameter]
	public MainLayout Parent { get; set; } = default!;

	[Inject]
	ServiceSettingsRepository ServiceSettingsRepository { get; set; } = default!;
	[Inject]
	NavigationManager NavigationManager { get; set; } = default!;
	[Inject]
	FluentValidation.IValidator<Palace.Shared.MicroServiceSettings> Validator { get; set; } = default!;
	[Inject]
	Orchestrator Orchestrator { get; set; } = default!;
	[Inject]
	IPackageRepository PackageRepository { get; set; } = default!;


	[Parameter]
	public string PalaceName { get; set; } = null!;
	[Parameter]
	public string ServiceName { get; set; } = null!;

	Palace.Shared.MicroServiceSettings serviceSettings = new();
	Pages.Components.CustomValidator customValidator = new();
	List<string> packageFileNameList = new();
	List<Palace.Shared.ArgumentsByHost> argumentsByHosts = new();

	protected override async Task OnInitializedAsync()
	{
		if (ServiceName != "new")
		{
			var data = await ServiceSettingsRepository.GetByServiceName(ServiceName);
			if (data == null)
			{
				NavigationManager.NavigateTo($"/servicelist");
				return;
			}
			serviceSettings = data;
		}

		var packageList = PackageRepository.GetPackageInfoList();
		packageFileNameList = packageList.Select(i => i.PackageFileName).ToList();

		if (serviceSettings.Id != Guid.Empty)
		{
			argumentsByHosts = await ServiceSettingsRepository.GetArgumentsByService(serviceSettings.Id);
			var currentHostNameList = Orchestrator.GetHostList().Select(i => i.HostName).ToList();
			var missingHosts = currentHostNameList.Except(argumentsByHosts.Select(i => i.HostName)).ToList();
			foreach (var hostName in missingHosts)
			{
				argumentsByHosts.Add(new Palace.Shared.ArgumentsByHost
				{
					Id = Guid.Empty,
					ServiceSettingId = serviceSettings.Id,
					HostName = hostName,
					Arguments = string.Empty
				});
			}
		}
	}

	async Task ValidateAndSave()
	{
		var result = await ServiceSettingsRepository.SaveServiceSettings(serviceSettings);
		if (!result.success)
		{
			customValidator.DisplayErrors(result.brokenRules);
			return;
		}
		await ServiceSettingsRepository.SaveArgumentsByHost(argumentsByHosts);

		Parent.ShowToast("Saved", $"Service {serviceSettings.ServiceName} saved", ToastLevel.Success);
	}
}