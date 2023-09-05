using Microsoft.Extensions.Configuration;

namespace Palace.Server.Services;

public interface IPalacePlugin
{
	string Name { get; }
	Task Configure(IServiceCollection services, IConfiguration configuration);
}
