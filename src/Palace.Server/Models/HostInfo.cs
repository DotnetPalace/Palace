namespace Palace.Server.Models;

public class HostInfo
{
    public string HostName { get; set; } = null!;
    public string MachineName { get; set; } = null!;
    public string ExternalIp { get; set; } = null!;
    public string MainFileName { get; set; } = null!;
    public string Version { get; set; } = null!;
    public long TotalDriveSize { get; set; }
    public long TotalFreeSpaceOfDriveSize { get; set; }
    public string? OsDescription { get; set; }
    public string? OsVersion { get; set; }
    public int ProcessId { get; set; }
    public double PercentCpu { get; set; }

    public DateTime CreationDate { get; set; } = DateTime.Now;
    public DateTime? LastHitDate { get; set; }
    public HostState HostState { get; set; } = HostState.Unknown;
}
