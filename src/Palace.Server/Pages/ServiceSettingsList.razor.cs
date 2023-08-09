using Microsoft.EntityFrameworkCore;

namespace Palace.Server.Pages;

public partial class ServiceSettingsList
{
    [Inject]
    IDbContextFactory<Services.PalaceDbContext> DbContextFactory { get; set; } = default!;
    [Inject]
    Services.ClipboardService ClipboardService { get; set; } = default!;
	[Inject]
	FluentValidation.IValidator<Palace.Shared.MicroServiceSettings> Validator { get; set; } = default!;


	string jsonServicesContent = string.Empty;
    Pages.Components.CustomValidator customValidator = new();
    Components.Toast toast = default!;
    List<Palace.Shared.MicroServiceSettings> serviceSettingsList = new();

    protected override async Task OnInitializedAsync()
    { 
        var db = await DbContextFactory.CreateDbContextAsync();
        serviceSettingsList = await db.MicroServiceSettings.ToListAsync();
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
		var validation = await Validator.ValidateAsync(settings);
		var db = await DbContextFactory.CreateDbContextAsync();
		db.MicroServiceSettings.Attach(settings);
		db.Entry(settings).State = EntityState.Modified;
		var changeCount = await db.SaveChangesAsync();
        toast.Show("Settings saved", ToastLevel.Info);
	}
}
