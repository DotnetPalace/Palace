using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Palace.Server.Services;

namespace Palace.Plugin._2FA;

public class PalacePlugin : IPalacePlugin
{
	public string Name => "2FA";

	public Task Configure(IServiceCollection services, IConfiguration configuration)
	{
		var section = configuration.GetRequiredSection("Palace.2FA");
		var settings = new _2FAConfiguration();
		section.Bind(settings);

		services.AddSingleton(settings);

		services.AddSingleton<ILoginService, LoginService>();
		return Task.CompletedTask;
	}
}