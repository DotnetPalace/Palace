using Palace.Server.Services;

namespace Palace.Server.Pages.Shared;

public partial class MainLayout
{
    [Inject]
    Palace.Server.Configuration.GlobalSettings GlobalSettings { get; set; } = default!;
    
    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

	[Inject]
	DialogService DialogService { get; set; } = default!;

    public event Action OnActionTerminated = default!;
	public event Action OnActionStarted = default!;

	protected override void OnInitialized()
    {
        var currentUri = new Uri(NavigationManager.Uri);
        GlobalSettings.CurrentUrl = $"{currentUri.Scheme}://{currentUri.Host}:{currentUri.Port}";

		DialogService.OnShowDialog += StateHasChanged;
		DialogService.OnCloseDialog += StateHasChanged;
	}

    public void ActionTerminated()
    {
        OnActionTerminated?.Invoke();
    }

	public void ActionStarted()
	{
		OnActionStarted?.Invoke();
	}

}