using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Palace.Shared;

using static System.Collections.Specialized.BitVector32;

namespace Palace.Secret.KeyVaultProvider;

public class SecretValueReader : ISecretValueReader
{
	private KeyVaultProviderConfiguration _configuration = new();

	public string Name => "AzureKeyVault";

	public void Configure(IServiceCollection services, IConfiguration configuration)
	{
		try
		{
			var section = configuration.GetRequiredSection("Palace.KeyVaultProvider");
			section.Bind(_configuration);
        }
		catch (Exception)
		{
            Console.WriteLine("Section Palace.KeyVaultProvider not found in configuration");
            return;
        }

        var vaultUri = new Uri($"https://{_configuration.KeyVaultName}.vault.azure.net");
		TokenCredential? credential = null;
		if ("Secret".Equals(_configuration.ClientMethod, StringComparison.InvariantCultureIgnoreCase))
		{
			credential = new ClientSecretCredential(_configuration.KeyVaultTenantId, _configuration.KeyVaultClientId, _configuration.KeyVaultClientSecret);
		}
		else if ("Certificat".Equals(_configuration.ClientMethod, StringComparison.InvariantCultureIgnoreCase))
		{
			credential = new ClientCertificateCredential(_configuration.KeyVaultTenantId, _configuration.KeyVaultClientId, _configuration.KeyVaultCertificatFileName);
		}

		var client = new SecretClient(vaultUri, credential);
		_configuration.SecretClient = client;

		services.AddSingleton<ISecretValueReader>(this);
	}

	public async Task<string> GetSecretValue(string secretName)
	{
		var secret = await _configuration.SecretClient.GetSecretAsync(secretName);
		return secret.Value.Value;
	}
}
