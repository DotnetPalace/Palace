﻿namespace Palace.Shared.Messages;

public class StopServiceReport : IMessage
{
    public Guid ActionSourceId { get; set; }
    public string ServiceName { get; set; } = null!;
    public string HostName { get; set; } = null!;
    public DateTime ActionDate { get; set; } = DateTime.Now;
    public ServiceState State { get; set; }
    public DateTime Timeout { get; set; } = DateTime.Now.AddMinutes(2);
    public string Origin { get; set; } = null!;
}