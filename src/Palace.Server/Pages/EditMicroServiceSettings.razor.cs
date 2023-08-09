using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Palace.Server.Pages;

public partial class EditMicroServiceSettings : ComponentBase
{
    [Inject] 
    IDbContextFactory<Services.PalaceDbContext> DbContextFactory { get; set; } = default!;
    [Inject] 
    NavigationManager NavigationManager { get; set; } = default!;
    [Inject]
    FluentValidation.IValidator<Palace.Shared.MicroServiceSettings> Validator { get; set; } = default!;
    [Inject]
    Services.Orchestrator Orchestrator { get; set; } = default!;

    [Parameter]
    public string PalaceName { get; set; } = null!;
    [Parameter]
    public string ServiceName { get; set; } = null!;

    Palace.Shared.MicroServiceSettings serviceSettings = new();
    Pages.Components.CustomValidator customValidator = new();
    bool _isNew = false;
    List<string> packageFileNameList = new();

    protected override async Task OnInitializedAsync()
    {
        var db = await DbContextFactory.CreateDbContextAsync();
        if (ServiceName == "new")
        {
            serviceSettings.Id = Guid.NewGuid();
            _isNew = true;
        }
        else
        {
            var data = db.MicroServiceSettings.FirstOrDefault(i => i.ServiceName == ServiceName);
            if (data == null)
            {
                NavigationManager.NavigateTo($"/servicelist");
                return;
            }
            serviceSettings = data;
        }

        var packageList = Orchestrator.GetPackageInfoList();
        packageFileNameList = packageList.Select(i => i.PackageFileName).ToList();
    }

    async Task ValidateAndSave()
    {
        Sanitize(serviceSettings);
        var validation = await Validator.ValidateAsync(serviceSettings);
        if (!validation.IsValid)
        {
            customValidator.DisplayErrors(validation);
            return;
        }
        var db = await DbContextFactory.CreateDbContextAsync();
        if (_isNew)
        {
            db.MicroServiceSettings.Add(serviceSettings);
            db.Entry(serviceSettings).State = EntityState.Added;
        }
        else
        {
            db.MicroServiceSettings.Attach(serviceSettings);
            db.Entry(serviceSettings).State = EntityState.Modified;
        }

        try
        {
            var changeCount = await db.SaveChangesAsync();
        }
        catch(Exception ex)
        {
            customValidator.DisplayErrors(ex);
        }
    }

    private void Sanitize(MicroServiceSettings serviceSettings)
    {
        serviceSettings.ServiceName = serviceSettings.ServiceName.Trim();
        serviceSettings.MainAssembly = serviceSettings.MainAssembly.TrimEnd('/');
        serviceSettings.Arguments = serviceSettings.Arguments?.Trim();
        serviceSettings.GroupName = serviceSettings.GroupName?.Trim();
        serviceSettings.PackageFileName = serviceSettings.PackageFileName.Trim();
    }
}