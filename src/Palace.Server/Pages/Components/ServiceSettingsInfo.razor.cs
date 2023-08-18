using Palace.Server.Pages.Shared;
using Palace.Server.Services;

namespace Palace.Server.Pages.Components;

public partial class ServiceSettingsInfo
{
	[CascadingParameter]
	public MainLayout Parent { get; set; } = default!;

	[Parameter]
    public Palace.Shared.MicroServiceSettings ServiceSettings { get; set; } = default !;

    [Parameter]
    public PackageInfo PackageInfo { get; set; } = default !;

    [Parameter]
    public EventCallback OnSettingSaved { get; set; } = default!;

	[Inject]
	ServiceSettingsRepository ServiceSettingsRepository { get; set; } = default!;

    bool isExpanded = false;

	async Task SaveServiceSettings()
    {
        var result = await ServiceSettingsRepository.SaveServiceSettings(ServiceSettings);
        if (!result.success)
        {
            Parent.ShowToast("Settings saved", $"save failed with message : {result.brokenRules.FirstOrDefault()?.ErrorMessage}", ToastLevel.Error);
        }
        else
        {
            Parent.ShowToast("Settings saved", "success", ToastLevel.Info);
			if (OnSettingSaved.HasDelegate)
			{
				await OnSettingSaved.InvokeAsync();
			}
		}
    }
}