using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Shared.Messages;

public class StartingServiceReport : IMessage
{
    public string HostName { get; set; } = null!;
    public string ServiceName { get; set; } = null!;
    public ServiceState ServiceState { get; set; }
    public string? FailReason { get; set; }
    public string? InstallationFolder { get; set; }
    public int ProcessId { get; set; }
    public string? CommandLine { get; set; }
    public DateTime Timeout => DateTime.Now.AddSeconds(15);
}
