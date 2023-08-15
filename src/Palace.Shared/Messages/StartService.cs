namespace Palace.Shared.Messages;

public class StartService : IMessage
{
    public required Guid ActionId { get; set; }
    public string HostName { get; set; } = null!;
    public MicroServiceSettings ServiceSettings { get; set; } = new();
    public string? OverridedArguments { get; set; }
    public DateTime Timeout { get; set; } = DateTime.Now.AddSeconds(15);
	public string Origin { get; set; } = null!;
}
