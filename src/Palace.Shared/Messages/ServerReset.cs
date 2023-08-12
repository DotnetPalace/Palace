using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Shared.Messages;

public class ServerReset : IMessage
{
	public DateTime Timeout => DateTime.Now.AddSeconds(15);
}
