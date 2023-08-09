using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DemoSvc1;

internal class PingWorker : BackgroundService
{
	private readonly ILogger<PingWorker> _logger;

	public PingWorker(ILogger<PingWorker> logger)
    {
		_logger = logger;
	}
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			_logger.LogTrace("ping at: {time}", DateTimeOffset.Now);
			if (!stoppingToken.IsCancellationRequested)
			{
				await Task.Delay(8 * 1000, stoppingToken);
			}
		}
	}
}
