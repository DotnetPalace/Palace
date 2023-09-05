using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Palace.Server.Configuration;

namespace Palace.Tests
{
	internal class PalaceServerApplication : WebApplicationFactory<global::Palace.WebApp.App>
    {
		private readonly GlobalSettings _settings;

		public PalaceServerApplication(Palace.Server.Configuration.GlobalSettings settings)
        {
			_settings = settings;
		}

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var currentDirectory = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location)!;
            var jsonFile = System.IO.Path.Combine(currentDirectory, "appSettings.json");

            var configuration = new ConfigurationBuilder()
                                        .AddJsonFile(jsonFile)
                                        .Build();

            builder.UseConfiguration(configuration);

            builder.ConfigureTestServices(services =>
            {
                // services.AddTransient<INotificationService, MockNotificationService>();
                // services.AddSingleton<IStartupFilter, MockStartupFilter>();
            });

            base.ConfigureWebHost(builder);
        }
    }
}
