using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Host.Configuration;

public class GlobalSettings : Shared.GlobalSettings
{
    public string UpdateFolder { get; set; } = @".\update";
    public string DownloadFolder { get; set; } = @".\download";
    public string InstallationFolder { get; set; } = @".\microservices";
    public string HostName { get; set; } = System.Environment.MachineName;

    public int ScanIntervalInSeconds { get; set; } = 20;
    public int WaitingUpdateTimeoutInSecond { get; set; } = 30;
    public bool StopAllMicroServicesWhenStop { get; set; } = false;
}
