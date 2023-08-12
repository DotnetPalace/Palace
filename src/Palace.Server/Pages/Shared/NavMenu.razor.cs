using Palace.Server.Services;

namespace Palace.Server.Pages.Shared;

public partial class NavMenu
{
	[Inject]
	Services.Orchestrator Orchestrator { get; set; } = default!;

	[Inject]
	DialogService DialogService { get; set; } = default!;

	async Task GlobalReset()
	{
		var confirm = await DialogService.Confirm("Global Reset", "Do you confirm the global reset ?");
		if (confirm)
		{
			Orchestrator.GlobalReset();
		}
	}
}