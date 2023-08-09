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
}
