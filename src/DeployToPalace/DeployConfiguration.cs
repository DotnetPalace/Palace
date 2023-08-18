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
}
