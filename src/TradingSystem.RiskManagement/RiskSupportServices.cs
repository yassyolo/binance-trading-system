using Microsoft.Extensions.Options;
using TradingSystem.BotRuntime.Configuration;

namespace TradingSystem.RiskManagement;

public sealed record RiskOrderSize(decimal Quantity, int Leverage, decimal? MaximumNotional);

public interface IRiskOrderSizingProvider
{
    Task<RiskOrderSize?> GetAsync(string botName, CancellationToken ct);
}

public sealed class ConfiguredRiskOrderSizingProvider(
    IBotRuntimeConfigurationProvider runtimeConfigurationProvider,
    IOptions<CentralRiskOptions> options) : IRiskOrderSizingProvider
{
    private readonly CentralRiskOptions _options = options.Value;

    public async Task<RiskOrderSize?> GetAsync(string botName, CancellationToken ct)
    {
        var runtime = await runtimeConfigurationProvider.GetAsync(botName, ct);
        if (runtime is not null && runtime.Quantity > 0 && runtime.Leverage > 0)
            return new RiskOrderSize(runtime.Quantity, runtime.Leverage, null);

        return _options.BotProfiles.TryGetValue(botName, out var profile)
            ? new RiskOrderSize(profile.Quantity, profile.Leverage, profile.MaximumNotional)
            : null;
    }
}
