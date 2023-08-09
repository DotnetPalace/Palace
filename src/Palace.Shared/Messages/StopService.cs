using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Shared.Messages;

public class StopService : IMessage
{
    public string ServiceName { get; set; } = null!;
    public string HostName { get; set; } = null!;
    public DateTime Timeout => DateTime.Now.AddSeconds(15);
}
