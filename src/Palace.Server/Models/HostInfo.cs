namespace Palace.Server.Models;

public class HostInfo
{
    public string HostName { get; set; } = null!;
    public string MachineName { get; set; } = null!;
    public string ExternalIp { get; set; } = null!;
    public string MainFileName { get; set; } = null!;
    public string Version { get; set; } = null!;

    public DateTime CreationDate { get; set; } = DateTime.Now;
    public DateTime? LastHitDate { get; set; }
    public HostState HostState { get; set; } = HostState.Unknown;
}
