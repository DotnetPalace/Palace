using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Shared;

public class ArgumentsByHost
{
    public Guid Id { get; set; }
	public Guid ServiceSettingId { get; set; }
    public string HostName { get; set; } = null!;
	public string Arguments { get; set; } = null!;
}
