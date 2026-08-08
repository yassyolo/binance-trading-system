using Microsoft.Extensions.Options;

namespace TradingSystem.StrategyPlugins.Configuration;

public sealed class StrategyPluginOptionsValidator : IValidateOptions<StrategyPluginOptions>
{
    public ValidateOptionsResult Validate(string? name, StrategyPluginOptions options)
    {
        var e = new List<string>();

        if (options.LoadExternalAssemblies && string.IsNullOrWhiteSpace(options.PluginDirectory))
            e.Add("StrategyPlugins:PluginDirectory is required when external assembly loading is enabled.");

        if (string.IsNullOrWhiteSpace(options.DefaultVersion) || !Version.TryParse(options.DefaultVersion, out _))
            e.Add("StrategyPlugins:DefaultVersion must be a valid version, for example 1.0.0.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}
