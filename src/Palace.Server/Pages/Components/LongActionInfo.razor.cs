using Palace.Server.Pages.Shared;

namespace Palace.Server.Pages.Components;

public sealed partial class LongActionInfo : ComponentBase, IDisposable
{
	[CascadingParameter]
	public MainLayout Parent { get; set; } = default!;

	[Parameter]
	public Models.LongAction LongAction { get; set; } = default!;

	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			LongAction.Completed += ActionCompleted;
			LongAction.LogList.ListChanged += AddLog;
		}
	}

    private void AddLog(object? sender, System.ComponentModel.ListChangedEventArgs e)
    {
		InvokeAsync(StateHasChanged);
    }

    void CloseAction()
	{
		LongAction.Completed -= ActionCompleted;
		Parent.CloseLongAction(LongAction);
	}

	void ActionCompleted(Models.ActionResult message)
	{
		if (message.Success)
		{
			LongAction.LogList.Add(new Models.ActionLog
			{
				Color = "success",
				Message = "Action completed"
			});
			LongAction.BgColor = "bg-success";
		}
		else
		{
			LongAction.LogList.Add(new Models.ActionLog
			{
				Color = "warning",
				Message = message.FailReason
			});
			LongAction.BgColor = "bg-danger";
		}
		InvokeAsync(StateHasChanged);
	}

	public void Dispose()
	{
		LongAction.Completed -= ActionCompleted;
        LongAction.LogList.ListChanged -= AddLog;
        LongAction = null!;
	}
}