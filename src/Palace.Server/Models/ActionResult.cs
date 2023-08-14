namespace Palace.Server.Models;

public class ActionResult
{
	public Guid ActionId { get; set; }
	public bool Success { get; set; }
	public string? FailReason { get; set; }
}
