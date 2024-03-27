namespace Palace.Shared.Messages;

public class ServiceHealthCheck : IMessage
{
    public string HostName { get; set; } = null!;
    public RunningMicroserviceInfo ServiceInfo { get; set; } = new();
    public DateTime Timeout { get; set; } = DateTime.Now.AddSeconds(30);
}
