namespace Palace.Shared.Messages;

public class HostStopped : IMessage
{
    public string HostName { get; set; } = null!;
    public string MachineName { get; set; } = null!;
    public DateTime Timeout { get; set; } = DateTime.Now.AddSeconds(60);
}
