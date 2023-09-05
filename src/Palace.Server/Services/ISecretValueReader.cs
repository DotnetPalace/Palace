using Microsoft.Extensions.Configuration;

namespace Palace.Server.Services;

public interface ISecretValueReader
{
	string Name { get; }
	Task<string> GetSecretValue(string secretName);
	void Configure(IServiceCollection services, IConfiguration configuration);
}
