namespace Palace.Server.Services;

public class DialogService
{
    private readonly List<TaskCompletionSource<object?>> _dialogList = new();
    private readonly List<TaskCompletionSource<bool>> _confirmList = new();

    public Type? DialogType { get; set; }
    public Dictionary<string, object?> Parameters { get; set; } = new();

    public event Action OnShowDialog = default!;
    public event Action OnCloseDialog = default!;

    public Task<object?> ShowAsync<T>(Dictionary<string, object?>? parameters = null)
        where T : ComponentBase
    {
        DialogType = typeof(T);
        Parameters = parameters ?? new Dictionary<string, object?>();
        OnShowDialog?.Invoke();
        var tcs = new TaskCompletionSource<object?>();
        _dialogList.Add(tcs);
        return tcs.Task;
    }

    public Task<bool> Confirm(string title, string message, bool allowBypassConfirmation = false)
    {
        DialogType = typeof(ConfirmDialog);
        Parameters = new Dictionary<string, object?>
        {
            { "Title", title },
            { "Question" , message },
            { "AllowBypassConfirmation", allowBypassConfirmation }
        };
        OnShowDialog?.Invoke();
        var tcs = new TaskCompletionSource<bool>();
        _confirmList.Add(tcs);
        return tcs.Task;
    }

    public async Task Alert(string title, string body)
    {
        await Task.Yield();
    }

    public void CloseConfirm(bool result)
    {
        DialogType = null;
        Parameters = new();
        var tcs = _confirmList.LastOrDefault();
        if (tcs is not null
            && !tcs.Task.IsCompleted)
        {
            tcs.SetResult(result);
            _confirmList.Remove(tcs);
        }
        OnCloseDialog?.Invoke();
    }

    public void Close(object? result = null)
    {
        DialogType = null;
        Parameters = new();
        var tcs = _dialogList.LastOrDefault();
        if (tcs is not null
            && !tcs.Task.IsCompleted)
        {
            tcs.SetResult(result);
            _dialogList.Remove(tcs);
        }
        OnCloseDialog?.Invoke();
    }
}
