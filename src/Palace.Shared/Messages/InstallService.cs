using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Shared.Messages;

public class InstallService : IMessage
{
    public string HostName { get; set; } = null!;
    public MicroServiceSettings ServiceSettings { get; set; } = new();
    public string DownloadUrl { get; set; } = null!;
    public string Trigger { get; set; } = null!;
    public DateTime Timeout => DateTime.Now.AddSeconds(15);
}
