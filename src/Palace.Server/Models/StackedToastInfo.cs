namespace Palace.Server.Models;

public class StackedToastInfo
{
	public Action OnClose { get; set; } = default!;
	public Guid Id { get; set; } = Guid.NewGuid();
	public LongAction LongAction { get; set; } = default!;
	public string TypeName { get; set; } = null!;
	public string Title { get; set; } = null!;
	public string Content { get; set; } = null!;
	public int? DisplayInSecond { get; set; }

}