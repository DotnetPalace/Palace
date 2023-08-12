namespace Palace.Client;

public class PalaceSettings
{
    public string ServiceName { get; set; } = null!;
    public string HostEnvironmentName { get; set; } = null!;
    public string HostName { get; set; } = System.Environment.MachineName;

    public string AzureBusConnectionString { get; set; } = null!;
    public string? QueuePrefix { get; set; }

    public string StopTopicName { get; set; } = "palace.stopservice";
	public string ServiceHealthQueueName { get; set; } = "palace.servicehealth";
    public string StopServiceReportQueueName { get; set; } = "palace.stopservicereport";

	public int TimeoutInSecondBeforeKillService { get; set; } = 15;
	public int ScanIntervalInSeconds { get; set; } = 15;

	internal string MachineName { get; set; } = System.Environment.MachineName;
	internal string Key => $"{HostName}-{ServiceName}";

}
