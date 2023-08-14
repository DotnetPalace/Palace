using System.ComponentModel;
using System.Diagnostics.Tracing;

using Microsoft.AspNetCore.Mvc;

namespace Palace.Server.Services;

public class LongActionService
{
    public readonly BindingList<Models.LongAction> _longActionList = new();
	private readonly ILogger _logger;

	public LongActionService(ILogger<LongActionService> logger)
    {
        _longActionList.ListChanged += (s, arg) =>
        {
            ActionListChanged?.Invoke(arg);
		};
		_logger = logger;
	}

    public event Action<ListChangedEventArgs> ActionListChanged = default!;
    public event Action<Models.LongAction> ActionItemAdded = default!;

    public void InsertAction(Models.LongAction item)
    {
        _longActionList.Insert(0, item);    
        ActionItemAdded?.Invoke(item);
    }

    public async Task StartAction(Models.LongAction item)
	{
        try
        {
            await item.Action.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
	}

	public void RemoveAction(Models.LongAction action)
	{
		_longActionList.Remove(action);
        action.Dispose();
	}

	public async Task SetActionCompleted(Models.ActionResult actionResult)
	{
		await Task.Delay(500);

		var items = _longActionList.Where(i => i.Id == actionResult.ActionId
						&& !i.CompletedDate.HasValue).ToList();

		foreach (var item in items)
		{
			item.OnCompleted(actionResult);
		}
	}

	public async Task AddLog(Guid actionId, Models.ActionLog log)
    {
        await Task.Delay(500);

        var items = _longActionList.Where(i => i.Id == actionId
                        && !i.CompletedDate.HasValue).ToList();

        foreach (var item in items)
        {
            item.LogList.Add(log);
        }
    }

	public bool IsActionRunning(Guid taskId)
	{
        var items = _longActionList.Where(i => i.Id == taskId && !i.CompletedDate.HasValue);
		return !items.Any();
	}

}
