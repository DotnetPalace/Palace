using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Shared.Messages;

public class UnInstallService : IMessage
{
    public string HostName { get; set; } = default!;
    public MicroServiceSettings ServiceSettings { get; set; } = default!;
    public DateTime Timeout => DateTime.Now.AddSeconds(15);
}
