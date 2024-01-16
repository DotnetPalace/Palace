namespace Palace.Server.Models;

public class ExtendedMicroServiceInfo : RunningMicroserviceInfo
{
    public DateTime CreationDate { get; set; } = DateTime.Now;
    public bool UIDisplayMore { get; set; } = false;
    public string? FailReason { get; set; }
    public string? Log { get; set; }
    public List<PerformanceCounter> ThreadCountHistory { get; set; } = new();
    public List<PerformanceCounter> WorkingSetHistory { get; set; } = new();

    public string Key => $"{HostName}||{ServiceName}".ToLower();
}
