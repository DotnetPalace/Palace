using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;

using Palace.Server.Services;

namespace Palace.Server;

public static class PluginLoader
{
    private static List<Assembly> _assemblies = new List<Assembly>();

    public static IEnumerable<Assembly> GetPluginAssemblies()
    {
        return _assemblies;
    }

    public static async Task LoadPlugins(WebApplicationBuilder builder)
    {
        var currentFolder = System.IO.Path.GetDirectoryName(typeof(PluginLoader).Assembly.Location)!;
        var pluginAssemblies = System.IO.Directory.GetFiles(currentFolder, $"Palace*.dll");
        foreach (var secretAssemblyFile in pluginAssemblies)
        {
            Assembly.LoadFrom(secretAssemblyFile);
        }
        var palaceAssemblies = AppDomain.CurrentDomain
                                .GetAssemblies()
                                .Where(a => a.FullName!.StartsWith($"Palace"));

        var pluginList = palaceAssemblies
                                .SelectMany(i => i.GetTypes()
                                .Where(i => !i.IsInterface && typeof(IPalacePlugin).IsAssignableFrom(i)));

        foreach (var item in pluginList)
        {
            var plugin = (IPalacePlugin)Activator.CreateInstance(item)!;
            await plugin.Configure(builder.Services, builder.Configuration);
            _assemblies.Add(plugin.GetType().Assembly);
        }
    }
}
