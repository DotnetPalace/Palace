# Palace V2.0

µService hoster suite

## What is Palace?

Palace is a µService hoster suite. It is designed to be a simple, yet powerful, way to host µServices.

- No docker
- No swarm
- No kubernetes
- No Podman
- No virtualization
- No containers- Only 1 zip file by µService with a very small size versus docker imageRequierment : - Palace only works with dotnet core applications (V6+).
- 1 Azure bus account

## What does Palace allow you to do ?

- Palace allows for on-the-fly installation of µServices on any server where a host is installed.
- Palace allows for the rapid deployment and updating of microservices on any server where a host is installed, even if they are not in operation.
- Palace also monitors both the hosts and the services and alerts in case of failure.
- Thanks to Azure Bus, Palace can bypass all firewalls; there's no need for a specific network.
- By default, Palace is multi-datacenter and multi-cloud.
- In case of a failure during an update, a rollback procedure is provided.

## How using Palace ?

Add this nuget package 

> Palace V2

Add this parameter in Program of your µService like :

``` c#
services.AddPalaceClient(config =>
{
	config.ServiceName = "YourServiceName";
	config.AzureBusConnectionString = "YourAzureBusConnectionString";
	config.StopServiceReportQueueName = "palace.stopservicereport";
	config.ServiceHealthQueueName = "palace.servicehealth";
	config.StopTopicName = "palace.stopservice";
	config.HostEnvironmentName = "Production";
});
```

- Install Palace.Host in your serveurs as Windows Service

- Install Palace.Server in your server as IIS Web Site


 



 




