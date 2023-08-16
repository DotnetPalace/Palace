using Azure.Security.KeyVault.Secrets;

namespace Palace.Secret.KeyVaultProvider;

public class KeyVaultProviderConfiguration
{
	public string KeyVaultTenantId { get; set; } = null!;
	public string KeyVaultClientId { get; set; } = null!;
	public string KeyVaultName { get; set; } = null!;
	public string KeyVaultClientSecret { get; set; } = null!;
	public string KeyVaultCertificatFileName { get; set; } = null!;
	public string ClientMethod { get; set; } = "Secret"; // "Certificat"

    internal SecretClient SecretClient { get; set; } = default!;
}