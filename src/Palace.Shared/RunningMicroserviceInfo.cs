namespace Palace.Shared;

public class RunningMicroserviceInfo
{
    public string ServiceName { get; set; } = null!;
    public string HostName { get; set; } = null!;
    public string Version { get; set; } = null!;
    public string? Location { get; set; }
    public bool UserInteractive { get; set; }
    public DateTime LastWriteTime { get; set; }
    public int ThreadCount { get; set; }
    public int ProcessId { get; set; }
    public ServiceState ServiceState { get; set; }
    public DateTime StartedDate { get; set; }
    public DateTime? LastHitDate { get; set; }
    public string CommandLine { get; set; } = null!;
    public long PeakWorkingSet { get; set; }
    public long WorkingSet { get; set; }
    public long PeakPagedMem { get; set; }
    public long PeakVirtualMem { get; set; }
    public string EnvironmentName { get; set; } = null!;
}