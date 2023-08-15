namespace Palace.Shared.Messages;

public class StopService : IMessage
{
    public required Guid ActionId { get; set; }
    public required string ServiceName { get; set; } = null!;
    public required string HostName { get; set; } = null!;
    public DateTime Timeout { get; set; } = DateTime.Now.AddMinutes(1);
    public required string Origin { get; set; } = null!;
}
