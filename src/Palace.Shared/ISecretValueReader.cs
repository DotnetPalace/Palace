using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Palace.Shared;

public interface ISecretValueReader
{
	string Name { get; }
	Task<string> GetSecretValue(string secretName);
	void Configure(IServiceCollection services, IConfiguration configuration);
}
