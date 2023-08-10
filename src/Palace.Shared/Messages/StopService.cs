namespace Palace.Shared.Messages;

public class StopService : IMessage
{
    public string ServiceName { get; set; } = null!;
    public string HostName { get; set; } = null!;
    public DateTime Timeout => DateTime.Now.AddSeconds(15);
}
