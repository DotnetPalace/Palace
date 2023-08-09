using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalaceDeployCli;

[Verb("prompt", isDefault: true, HelpText = "interactive mode")]
public class DefaultOptions
{

}

[Verb("status", HelpText = "Affiche le status d'une ip failover")]
public class StatusOptions
{
    [Option('i', "ip", Required = true)]
    public string Ip { get; set; }
}
