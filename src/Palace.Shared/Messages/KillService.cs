namespace Palace.Shared.Messages;

public class KillService : IMessage
{
	public required Guid ActionId { get; set; }
	public required string HostName { get; set; } = null!;
    public required int ProcessId { get; set; }
    public required MicroServiceSettings ServiceSettings { get; set; } = new();
	public DateTime Timeout { get; set; } = DateTime.Now.AddSeconds(60);
}
