using System.ComponentModel;

namespace Palace.Server.Models;

public sealed class LongAction : IDisposable
{
	public event Action<ActionResult>? Completed;

	public Guid Id { get; set; } = Guid.NewGuid();
	public required string Name { get; set; } = null!;
	public DateTime? CompletedDate { get; set; }
	public required Func<Task> Action { get; set; } = default!;
	public required string Title { get; set; } = null!;
    public string? Description { get; set; }
	public BindingList<ActionLog> LogList { get; set; } = new();
	public string BgColor { get; set; } = null!;
	public string TextColor { get; set; } = null!;

	public void OnCompleted(ActionResult actionResult)
	{
		if (CompletedDate is not null)
		{
			return;
		}
		CompletedDate = DateTime.Now;
		Completed?.Invoke(actionResult);
	}

	public void Dispose()
	{ 
		if (Completed is not null)
		{
			Completed = null;
		}
		Action = null!;
	}
}
