﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Shared.Messages;

public class StopServiceReport : IMessage
{
    public string ServiceName { get; set; } = null!;
    public string HostName { get; set; } = null!;
    public DateTime ActionDate { get; set; } = DateTime.Now;
    public ServiceState State { get; set; }
    public DateTime Timeout => DateTime.Now.AddSeconds(15);
}