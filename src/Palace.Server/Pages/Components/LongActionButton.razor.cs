using Microsoft.AspNetCore.Components.Web;

using Palace.Server.Pages.Shared;

namespace Palace.Server.Pages.Components;

public sealed partial class LongActionButton : ComponentBase, IDisposable
{
	[CascadingParameter]
	public MainLayout Parent { get; set; } = default!;

	[Parameter]
	public string ButtonType { get; set; } = "btn-primary";

	[Parameter]
	public bool IsExclusive { get; set; } = false;

	[Parameter]
	public Func<Task<Models.LongAction?>> CreateLongAction { get; set; } = default!;

	[Parameter(CaptureUnmatchedValues = true)]
	public Dictionary<string, object> CatpuredAttributes { get; set; } = new();

	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	string? disabled = null;
	bool showLoader = false;


	async Task Click(MouseEventArgs args)
	{
		disabled = "disabled";
		showLoader = true;
		StateHasChanged();
		var longAction = await CreateLongAction.Invoke();
		if (longAction is not null)
		{
			await Parent.StartLongAction(longAction);
		}
	}

	public void Dispose()
	{
		if (Parent is not null)
		{
		}
		GC.SuppressFinalize(this);
	}
}