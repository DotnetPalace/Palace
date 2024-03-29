﻿namespace Palace.Shared;

public class GlobalSettings
{
    public string AzureBusConnectionString { get; set; } = null!;
    public string? QueuePrefix { get; set; }
    public Guid ApiKey { get; set; }

    public string SecretConfigurationReaderName { get; set; } = "NoSecret";
    public string PackageDownloaderServiceName { get; set; } = "Local";

    public string HostHealthCheckQueueName { get; set; } = "palace.hosthealthcheck";
    public string HostStoppedQueueName { get; set; } = "palace.hoststopped";

    public string InstallServiceTopicName { get; set; } = "palace.installservice";
	public string InstallationReportQueueName { get; set; } = "palace.installationreport";

	public string UnInstallServiceTopicName { get; set; } = "palace.uninstallservice";
    public string UnInstallationReportQueueName { get; set; } = "palace.uninstallationreport";

	public string KillServiceTopicName { get; set; } = "palace.killservice";
	public string KillServiceReportQueueName { get; set; } = "palace.killservicereport";


	public string StartServiceTopicName { get; set; } = "palace.startservice";

	public string StopServiceTopicName { get; set; } = "palace.stopservice";
	public string StopServiceReportQueueName { get; set; } = "palace.stopservicereport";

	public string StartingServiceReportQueueName { get; set; } = "palace.startingservicereport";
    public string ServiceHealthQueueName { get; set; }= "palace.servicehealth";

    public string ServerResetTopicName { get; set; } = "palace.serverreset";
}
