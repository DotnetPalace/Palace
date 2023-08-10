using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Shared.Messages;

public class ServiceUnInstallationReport : IMessage
{
	public string HostName { get; set; } = null!;
	public string ServiceName { get; set; } = null!;
	public DateTime Timeout => DateTime.Now.AddSeconds(15);
	public bool Success { get; set; }
	public string? FailReason { get; set; }

}
