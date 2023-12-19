using Microsoft.EntityFrameworkCore;
using Palace.WebApp.Services;

namespace Palace.WebApp.Pages;

public partial class ServiceSettingsList
{
    [Inject]
    ServiceSettingsRepository ServiceSettingsRepository { get; set; } = default!;
    [Inject]
    ClipboardService ClipboardService { get; set; } = default!;
	[Inject]
	FluentValidation.IValidator<Palace.Shared.MicroServiceSettings> Validator { get; set; } = default!;


	string jsonServicesContent = string.Empty;
    Pages.Components.CustomValidator customValidator = new();
    Components.Toast toast = default!;
    List<Palace.Shared.MicroServiceSettings> serviceSettingsList = new();

    protected override async Task OnInitializedAsync()
    {
        serviceSettingsList = (await ServiceSettingsRepository.GetAll()).ToList();
	}

    async Task CopyToClipboard(object item)
	{
		var content = System.Text.Json.JsonSerializer.Serialize(item, new System.Text.Json.JsonSerializerOptions
		{
			WriteIndented = true
		});
        await ClipboardService.WriteTextAsync(content);
        await Task.Delay(2 * 1000);
        StateHasChanged();		
	}

    async Task SaveSettings(MicroServiceSettings settings)
    {
        var result = await ServiceSettingsRepository.SaveServiceSettings(settings);
        if (!result.success)
        {
			toast.Show($"save failed with message : {result.brokenRules.FirstOrDefault()?.ErrorMessage}", ToastLevel.Error);
		}
		else
        {
			toast.Show("Settings saved", ToastLevel.Info);
		}
	}

    async Task RemoveSettings(MicroServiceSettings settings)
    {
        var result = await ServiceSettingsRepository.RemoveServiceSettings(settings);
        if (!result)
        {
            toast.Show($"remove failed", ToastLevel.Error);
        }
    }
}
