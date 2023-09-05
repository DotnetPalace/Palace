namespace Palace.WebApp.Pages.Components;

public sealed partial class ConfirmDialog : ComponentBase, IDisposable
{
    [Inject]
    Services.DialogService DialogService { get; set; } = default!;

    [Parameter]
    public bool AllowBypassConfirmation { get; set; } = false;

    [Parameter]
    public string Title { get; set; } = "Confirmation";

    [Parameter]
    public string? Question { get; set; }

    MarkupString body;
    string modalDisplay = "block;";
    string modalClass = "show";

    protected override void OnInitialized()
    {
        if (AllowBypassConfirmation)
        {
            return;
        }
        body = new MarkupString($"{Question}".Replace("\r", "<br/>"));
    }

    async Task Confirm(bool confirm)
    {
        await Task.Yield();
        modalDisplay = "none";
        modalClass = "";
        DialogService.CloseConfirm(confirm);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
