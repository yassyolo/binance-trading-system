using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.StrategyPlugins.Catalog;
using TradingSystem.StrategyPlugins.Configuration;
using TradingSystem.StrategyPlugins.Resolver;

namespace TradingSystem.StrategyPlugins.Loading;

public static class StrategyPluginLoader
{
    public static IServiceCollection AddStrategyPluginSystem(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(StrategyPluginOptions.SectionName);
        var options = section.Get<StrategyPluginOptions>() ?? new StrategyPluginOptions();

        services.AddOptions<StrategyPluginOptions>()
            .Bind(section)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<StrategyPluginOptions>, StrategyPluginOptionsValidator>();

        if (options.Enabled && options.LoadExternalAssemblies)
            LoadExternalModules(services, configuration, options);

        services.AddSingleton<IStrategyPluginCatalog, StrategyPluginCatalog>();
        services.AddSingleton<ITradingStrategyResolver, TradingStrategyResolver>();
        return services;
    }

    private static void LoadExternalModules(
        IServiceCollection services,
        IConfiguration configuration,
        StrategyPluginOptions options)
    {
        var directory = Path.GetFullPath(
            options.PluginDirectory,
            AppContext.BaseDirectory);

        if (!Directory.Exists(directory))
        {
            if (options.FailOnPluginLoadError)
            {
                throw new DirectoryNotFoundException(
                    $"Strategy plugin directory '{directory}' does not exist.");
            }

            return;
        }

        foreach (var path in Directory.EnumerateFiles(
                     directory,
                     "*.dll",
                     SearchOption.TopDirectoryOnly))
        {
            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
                foreach (var type in GetLoadableTypes(assembly)
                             .Where(type =>
                                 !type.IsAbstract &&
                                 typeof(IStrategyPluginModule).IsAssignableFrom(type)))
                {
                    if (Activator.CreateInstance(type) is not IStrategyPluginModule module)
                    {
                        throw new InvalidOperationException(
                            $"Could not create strategy plugin module '{type.FullName}'. " +
                            "A public parameterless constructor is required.");
                    }

                    module.ConfigureServices(services, configuration);
                    services.AddSingleton(typeof(IStrategyPluginModule), module);
                }
            }
            catch when (!options.FailOnPluginLoadError)
            {
                // The host is explicitly configured to continue without optional plugins.
            }
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
