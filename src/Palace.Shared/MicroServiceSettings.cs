namespace Palace.Shared;

public class MicroServiceSettings
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string MainAssembly { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public bool AlwaysStarted { get; set; } = true;
    public string PackageFileName { get; set; } = string.Empty;

    public string? GroupName { get; set; } 

    public int? ThreadLimitBeforeRestart { get; set; }
    public int? ThreadLimitBeforeAlert { get; set; }

    public int? NoHealthCheckCountBeforeRestart { get; set; }
    public int? NoHealthCheckCountCountBeforeAlert { get; set; }

    public long? MaxWorkingSetLimitBeforeRestart { get; set; }
    public long? MaxWorkingSetLimitBeforeAlert { get; set; }
}
