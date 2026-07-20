using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TradingSystem.StrategyPlugins;

public static class StrategyPluginLoader
{
    public static IServiceCollection AddStrategyPluginSystem(this IServiceCollection services,  IConfiguration configuration)
    {
        var options  =  configuration.GetSection(StrategyPluginOptions.SectionName).Get<StrategyPluginOptions>() ?? new();
        services.Configure<StrategyPluginOptions>(configuration.GetSection(StrategyPluginOptions.SectionName));

        if (options.Enabled  &&  options.LoadExternalAssemblies)
            LoadExternalModules(services,  configuration,  options);

        services.AddSingleton<IStrategyPluginCatalog,  StrategyPluginCatalog>();
        services.AddSingleton<ITradingStrategyResolver,  TradingStrategyResolver>();
        return services;
    }

    private static void LoadExternalModules(IServiceCollection services,  IConfiguration configuration,  StrategyPluginOptions options)
    {
        var directory  =  Path.GetFullPath(options.PluginDirectory,  AppContext.BaseDirectory);
        if (!Directory.Exists(directory))
            return;

        foreach (var path in Directory.EnumerateFiles(directory,  "*.dll",  SearchOption.TopDirectoryOnly))
        {
            try
            {
                var assembly  =  AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
                foreach (var type in assembly.GetTypes().Where(x  =>  !x.IsAbstract  &&  typeof(IStrategyPluginModule).IsAssignableFrom(x)))
                {
                    if (Activator.CreateInstance(type) is not IStrategyPluginModule module)
                        throw new InvalidOperationException($"Could not create strategy plugin module '{type.FullName}'. A public parameterless constructor is required.");
                    module.ConfigureServices(services,  configuration);
                    services.AddSingleton(typeof(IStrategyPluginModule),  module);
                }
            }
            catch when (!options.FailOnPluginLoadError)
            {
                // Optional plugin failures are ignored only when explicitly configured.
            }
        }
    }
}
