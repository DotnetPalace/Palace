namespace Palace.Server.Pages;

public partial class PackageList
{
    [Inject]
    ILogger<PackageList> Logger { get; set; } = default!;
    [Inject]
    Services.Orchestrator Orchestrator { get; set; } = default!;
    [Inject] 
    Configuration.GlobalSettings GloblaSettings { get; set; } = default!;
    [Inject]
    Services.DialogService DialogService { get; set; } = default!;

    string? errorReport = null;
    List<Models.PackageInfo> availablePackageList = new();

    protected override void OnInitialized()
    {
        Orchestrator.PackageChanged += async (package) =>
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
        availablePackageList = Orchestrator.GetPackageInfoList().ToList();
    }


    async Task RemovePackage(string packageFileName)
    {
        var confirm = await DialogService.Confirm("Remove Package", "Are you sure to remove this package?");
        if (!confirm)
        {
            return;
        }
        var result = await Orchestrator.RemovePackage(packageFileName);
        if (result != null)
        {
            errorReport = result;
        }
        StateHasChanged();
    }
}
