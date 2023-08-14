using SQLitePCL;

namespace Palace.Server.Pages.Components;

public sealed partial class StackedToast : IDisposable
{
    [Parameter]
    public Models.StackedToastInfo StackedToastInfo { get; set; } = default!;

	[Parameter]
    public EventCallback Close { get; set; }

    public MarkupString Body
    {
        get
        {
            return new MarkupString(StackedToastInfo.Content);
        }
    }

    System.Timers.Timer _countdown = new();
    string display = "show";
    string toastStyle = "";

    protected override void OnParametersSet()
    {
        if (StackedToastInfo.DisplayInSecond.HasValue)
        {
            StartCountdown();
        }
        if (StackedToastInfo.TypeName == "error")
        {
            toastStyle = "text-white bg-danger";
		}
    }

    async Task OnClosed()
    {
        if (Close.HasDelegate)
        {
			await Close.InvokeAsync();
		}
		display = "hide";
		StackedToastInfo.OnClose?.Invoke();
        await InvokeAsync(StateHasChanged);
	}

	void StartCountdown()
    {
        SetCountdown();
        if (_countdown.Enabled)
        {
            _countdown.Stop();
        }

        _countdown.Start();
    }

    void SetCountdown()
    {
        if (_countdown == null)
        {
            _countdown = new System.Timers.Timer(StackedToastInfo.DisplayInSecond.GetValueOrDefault(5) * 1000);
            _countdown.Elapsed += HideToast;
            _countdown.AutoReset = false;
        }
    }

    async void HideToast(object? source, System.Timers.ElapsedEventArgs args)
    {
        display = "hide";
        await InvokeAsync(() =>
        {
			StackedToastInfo.OnClose?.Invoke();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        if (_countdown is not null)
        {
            _countdown.Stop();
            _countdown.Elapsed -= HideToast;
            _countdown.Dispose();
        }
    }
}