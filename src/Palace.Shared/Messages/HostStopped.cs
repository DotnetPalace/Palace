using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Shared.Messages;

public class HostStopped : IMessage
{
    public string HostName { get; set; } = null!;
    public string MachineName { get; set; } = null!;
    public DateTime Timeout => DateTime.Now.AddSeconds(15);
}
