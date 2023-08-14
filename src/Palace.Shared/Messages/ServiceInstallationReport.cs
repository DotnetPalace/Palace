﻿namespace Palace.Shared.Messages;

public class ServiceInstallationReport : IMessage
{
    public Guid ActionSourceId { get; set; }
    public string HostName { get; set; } = null!;
    public string ServiceName { get; set; } = null!;
    public bool Success { get; set; }
    public string? FailReason { get; set; }
    public string? InstallationFolder { get; set; }
    public DateTime ActionDate { get; set; } = DateTime.Now;
    public string Trigger { get; set; } = null!;
    public DateTime Timeout => DateTime.Now.AddSeconds(15);
}
