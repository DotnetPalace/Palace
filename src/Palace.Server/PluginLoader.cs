using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Server;

public static class PluginLoader
{
    public static async Task LoadPlugins(WebApplicationBuilder builder)
    {
        var currentFolder = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
        var secretAssemblies = System.IO.Directory.GetFiles(currentFolder, $"Palace*.dll");
        foreach (var secretAssemblyFile in secretAssemblies)
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
        }
    }
}
