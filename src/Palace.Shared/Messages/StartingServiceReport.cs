namespace Palace.Shared.Messages;

public class StartingServiceReport : IMessage
{
    public Guid ActionSourceId { get; set; }
    public string HostName { get; set; } = null!;
    public string ServiceName { get; set; } = null!;
    public ServiceState ServiceState { get; set; }
    public string? FailReason { get; set; }
    public string? InstallationFolder { get; set; }
    public int ProcessId { get; set; }
    public string? CommandLine { get; set; }
    public DateTime Timeout { get; set; } = DateTime.Now.AddSeconds(60);
    public string Origin { get; set; } = null!;
}
