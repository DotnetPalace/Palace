# Palace V2

dotnet µServices cluster manager

[![NuGet](https://img.shields.io/nuget/v/Palace.Client.svg)](https://www.nuget.org/packages/Palace.Client/)

![Dashboard](/Doc/Dashboard.png)

## What is Palace?

Palace is a µServices cluster manager. 

It is designed to be a very simple, yet powerful, way to host µServices without bullshit.

- No specific tooling or configuration like yaml files
- No Docker
- No Swarm
- No Kubernetes
- No Podman
- No Virtualization
- No Container
- Only 1 zip file by µService with a very small size vs docker image

Requierment : 

- Palace only works with dotnet core applications (V6+) in windows server (at moment).
- 1 Azure Bus mandatory account 

## What does Palace allow you to do ?

- Palace allows for on-the-fly installation of µServices on any server where a palace.host is installed.
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


 



 




