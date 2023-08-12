using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Shared.Messages;

public class HostHealthCheck : IMessage
{
    public string HostName { get; set; } = null!;
    public string MachineName { get; set; } = null!;
    public string ExternalIp { get; set; } = null!;
    public DateTime Now { get; set; } = DateTime.Now;
	public string MainFileName { get; set; } = null!;
	public string Version { get; set; } = null!;
    public DateTime Timeout => DateTime.Now.AddSeconds(15);
	public long TotalDriveSize { get; set; }
	public long TotalFreeSpaceOfDriveSize { get; set; }
	public string? OsDescription { get; set; }
	public string? OsVersion { get; set; }
    public int ProcessId { get; set; }
    public double PercentCpu { get; set; }
}
