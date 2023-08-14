using Palace.Server.Models;
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
	
	[Inject]
	LongActionService LongActionService { get; set; } = default!;

	public event Action OnActionTerminated = default!;
	public event Action OnActionStarted = default!;
	readonly List<StackedToastInfo> stackedToastList = new();

	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			DialogService.OnShowDialog += StateHasChanged;
			DialogService.OnCloseDialog += StateHasChanged;
			LongActionService.ActionItemAdded += LongActionItemAdded; 
		}
	}

	private void LongActionItemAdded(Models.LongAction item)
	{
		InvokeAsync(() =>
		{
			stackedToastList.Add(new StackedToastInfo
			{
				Id = item.Id,
				LongAction = item,
				TypeName = "longaction"
			});
			StateHasChanged();
		});
	}

	protected override void OnInitialized()
    {
        var currentUri = new Uri(NavigationManager.Uri);
        GlobalSettings.CurrentUrl = $"{currentUri.Scheme}://{currentUri.Host}:{currentUri.Port}";
	}

    public void ActionTerminated()
    {
        OnActionTerminated?.Invoke();
    }

	public void ActionStarted()
	{
		OnActionStarted?.Invoke();
	}

	public async Task StartLongAction(Models.LongAction item)
	{
		if (!LongActionService.IsActionRunning(item.Id))
		{
			await DialogService.Alert("Action", "This action already started");
			return;
		}
		LongActionService.InsertAction(item);
		StateHasChanged();
		await LongActionService.StartAction(item);
	}

	public void CloseLongAction(Models.LongAction longAction)
	{
		LongActionService.RemoveAction(longAction);
		stackedToastList.RemoveAll(i => i.Id == longAction.Id);
		longAction.Dispose();
		StateHasChanged();
	}

	void RemoveToast(Models.StackedToastInfo sti)
	{
		stackedToastList.Remove(sti);
		StateHasChanged();
	}
}