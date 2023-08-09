using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeployToPalace;

internal class DeployConfiguration
{
    public string EnvironmentName { get; set; } = null!;
    public string StagingUrl { get; set; } = null!;
    public string StagingFolder { get; set; } = null!;

    public List<string> CSProjFileNameList { get; set; } = new();

    public string KeyVaultTenantId { get; set; } = null!;
	public string KeyVaultClientId { get; set; } = null!;
	public string KeyVaultName { get; set; } = null!;
	public string KeyVaultClientSecret { get; set; } = null!;

}
