namespace Palace.Shared.Messages;

public class ServiceUnInstallationReport : IMessage
{
	public Guid ActionSourceId { get; set; }
	public string HostName { get; set; } = null!;
	public string ServiceName { get; set; } = null!;
	public DateTime Timeout { get; set; } = DateTime.Now.AddMinutes(3);
	public bool Success { get; set; }
	public string? FailReason { get; set; }

}
