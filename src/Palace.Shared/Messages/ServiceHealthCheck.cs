using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Shared.Messages;

public class ServiceHealthCheck : IMessage
{
    public string HostName { get; set; } = null!;
    public RunningMicroserviceInfo ServiceInfo { get; set; } = new();
    public DateTime Timeout => DateTime.Now.AddSeconds(15);
}
