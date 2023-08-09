namespace Palace.Server.Models;

public class PackageInfo
{
    public string PackageFileName { get; set; } = null!;
    public DateTime LastWriteTime { get; set; }
    public long Size { get; set; }
    public string? LockedBy { get; set; }
    public string CurrentVersion { get; set; } = null!;
    public bool ChangeDetected { get; set; }
}
