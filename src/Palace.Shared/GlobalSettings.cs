namespace Palace.Shared;

public class GlobalSettings
{
    public string AzureBusConnectionString { get; private set; } = null!;
    public void SetAzureBusConnectionString(string connectionString) => AzureBusConnectionString = connectionString;

    public Guid ApiKey { get; private set; }
    public void SetApiKey(Guid guid) => ApiKey = guid;

    public string KeyVaultTenantId { get; set; } = null!;
    public string KeyVaultClientId { get; set; } = null!;
    public string KeyVaultName { get; set; } = null!;
    public string KeyVaultClientSecret { get; set; } = null!;

    public string HostHealthCheckQueueName { get; set; } = "palace.hosthealthcheck";
    public string HostStoppedQueueName { get; set; } = "palace.hoststopped";

    public string InstallServiceTopicName { get; set; } = "palace.installservice";
    public string InstallationReportQueueName { get; set; } = "palace.installationreport";

    public string StartServiceTopicName { get; set; } = "palace.startservice";

	public string StopServiceTopicName { get; set; } = "palace.stopservice";
	public string StopServiceReportQueueName { get; set; } = "palace.stopservicereport";

	public string StartingServiceReportQueueName { get; set; } = "palace.startingservicereport";
    public string ServiceHealthQueueName { get; set; }= "palace.servicehealth";
}
