using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;

using Palace.Server.Pages.Shared;

namespace Palace.Server.Pages.Components;

public sealed partial class ActionButton : ComponentBase, IDisposable
{
	[CascadingParameter]
	public MainLayout Parent { get; set; } = default!;

	[Parameter]
	public string ButtonType { get; set; } = "btn-primary";

    [Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	[Parameter]
	public bool IsExclusive { get; set; } = false;

	[Parameter(CaptureUnmatchedValues = true)]
	public Dictionary<string, object> CatpuredAttributes { get; set; } = new();

	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	string? disabled = null;
	bool showLoader = false;

	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender
			&& Parent != null)
		{
			Parent.OnActionTerminated += OnActionTerminated;
			Parent.OnActionStarted += OnActionStarted;
		}
	}

	private void OnActionTerminated()
	{
		disabled = null;
		InvokeAsync(StateHasChanged);
	}

	private void OnActionStarted()
	{
		disabled = "disabled";
		InvokeAsync(StateHasChanged);
	}
	async Task Click(MouseEventArgs args)
	{
		disabled = "disabled";
		showLoader = true;
		StateHasChanged();
		if (IsExclusive)
		{
			Parent.ActionStarted();
		}
		if (OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync(args);
		}
	}

	public void Dispose()
	{
		if (Parent is not null)
		{
			Parent.OnActionTerminated -= OnActionTerminated;
			Parent.OnActionStarted -= OnActionStarted;
		}
		GC.SuppressFinalize(this);
	}
}