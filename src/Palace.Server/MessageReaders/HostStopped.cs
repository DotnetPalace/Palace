using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Palace.Server.Services;

namespace Palace.Server.MessageReaders;
internal class HostStopped(
	Microsoft.Extensions.Logging.ILogger<HostStopped> logger,
	Orchestrator orchestrator
	) 
	: ArianeBus.MessageReaderBase<Shared.Messages.HostStopped>
{
	public override async Task ProcessMessageAsync(Shared.Messages.HostStopped message, CancellationToken cancellationToken)
	{
		await Task.Yield();
		logger.LogInformation("HostStopped message received for {HostName} on {MachineName}", message.HostName, message.MachineName);
		// On recherche le host dans l'orchestrator
		var host = orchestrator.GetHostList().FirstOrDefault(h => h.HostName == message.HostName
																&& h.MachineName == message.MachineName);

		if (host is null)
		{
			logger.LogWarning("Host {HostName} on {MachineName} not found", message.HostName, message.MachineName);
			return;
		}

		host.HostState = HostState.Down;

		orchestrator.AddOrUpdateHost(host);
	}
}
