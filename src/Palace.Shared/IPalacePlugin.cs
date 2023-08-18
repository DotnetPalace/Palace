using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Palace.Shared;

public interface IPalacePlugin
{
    string Name { get; }
    Task Configure(IServiceCollection services, IConfiguration configuration);
}
