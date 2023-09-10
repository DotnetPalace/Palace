namespace Palace.Shared.Messages;
public class KillServiceReport : IMessage
{
	public Guid ActionSourceId { get; set; }
	public string HostName { get; set; } = null!;
	public string ServiceName { get; set; } = null!;
	public bool Success { get; set; }
	public string? FailReason { get; set; }
	public DateTime ActionDate { get; set; } = DateTime.Now;
	public DateTime Timeout { get; set; } = DateTime.Now.AddMinutes(2);

}
