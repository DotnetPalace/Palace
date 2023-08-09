using Palace.Server.Services;

namespace Palace.Server.Pages;

public partial class Package
{
    [Parameter]
    public string PackageFileName { get; set; } = null!;

    [Inject]
    ILogger<Package> Logger { get; set; } = default!;

    [Inject]
    Orchestrator Orchestrator { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    Services.DialogService DialogService { get; set; } = default!;

    Models.PackageInfo package = new();
    List<FileInfo> backupFileInfoList = new();
    string? errorReport { get; set; }


    protected override void OnInitialized()
    {
        var packageList = Orchestrator.GetPackageInfoList();
        var existing = packageList.FirstOrDefault(i => i.PackageFileName.Equals(PackageFileName, StringComparison.InvariantCultureIgnoreCase));
        if (existing is null)
        {
            NavigationManager.NavigateTo("/");
            return;
        }
        package = existing;
        backupFileInfoList = Orchestrator.GetBackupFileList(PackageFileName);
        base.OnInitialized();
    }

    async void RollbackPackage(FileInfo fileInfo)
    {
        var confirm = await DialogService.Confirm("Rollback", $"Confirm rollaback {fileInfo.Name} package ?");
        if (!confirm)
        {
            return;
        }

		var result = Orchestrator.RollbackPackage(package!, fileInfo);
        if (result != null)
        {
            errorReport = result;
        }
        NavigationManager.NavigateTo("/packages", true);
    }

}
