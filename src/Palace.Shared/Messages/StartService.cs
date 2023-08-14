namespace Palace.Shared.Messages;

public class StartService : IMessage
{
    public required Guid ActionId { get; set; }
    public string HostName { get; set; } = null!;
    public MicroServiceSettings ServiceSettings { get; set; } = new();
    public DateTime Timeout => DateTime.Now.AddSeconds(15);
    public string Origin { get; set; } = null!;
}
