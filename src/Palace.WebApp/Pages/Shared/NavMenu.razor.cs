using Microsoft.AspNetCore.Authentication;

using Palace.Server.Services;
using Palace.WebApp.Services;

namespace Palace.WebApp.Pages.Shared;

public partial class NavMenu
{
	[Inject]
	Orchestrator Orchestrator { get; set; } = default!;

	[Inject]
	DialogService DialogService { get; set; } = default!;

	[Inject]
	NavigationManager NavigationManager { get; set; } = default!;

	[Inject]
	ILoginService LoginService { get; set; } = default!;

    bool collapseNavMenu = true;
    string version = "1.0.0.0";
    string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    protected override void OnInitialized()
    {
        version = $"{typeof(Program).Assembly.GetName()?.Version}";
        base.OnInitialized();
    }

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }


    async Task GlobalReset()
	{
		var confirm = await DialogService.Confirm("Global Reset", "Do you confirm the global reset ?");
		if (confirm)
		{
			Orchestrator.GlobalReset();
		}
	}

	void SignOut()
	{
		NavigationManager.NavigateTo("?logout=1", true);
	}
}