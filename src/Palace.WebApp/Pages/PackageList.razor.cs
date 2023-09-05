using Palace.Server.Services;

namespace Palace.WebApp.Pages;

public partial class PackageList
{
    [Inject]
    ILogger<PackageList> Logger { get; set; } = default!;
    [Inject] 
    Palace.Server.Configuration.GlobalSettings GloblaSettings { get; set; } = default!;
    [Inject]
    Services.DialogService DialogService { get; set; } = default!;
    [Inject]
    IPackageRepository PackageRepository { get; set; } = default!;

    string? errorReport = null;
    List<PackageInfo> availablePackageList = new();

    protected override void OnInitialized()
    {
        PackageRepository.PackageChanged += async (package) =>
        {
            await InvokeAsync(() =>
            {
                UpdateLists();
                base.StateHasChanged();
            });
        };
        UpdateLists();
    }

    void UpdateLists()
    {
        availablePackageList = PackageRepository.GetPackageInfoList().ToList();
    }


    async Task RemovePackage(string packageFileName)
    {
        var confirm = await DialogService.Confirm("Remove Package", "Are you sure to remove this package?");
        if (!confirm)
        {
            return;
        }
        var result = await PackageRepository.RemovePackage(packageFileName);
        if (result != null)
        {
            errorReport = result;
        }
        StateHasChanged();
    }
}
