using Microsoft.Extensions.Options;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.RiskManagement.Configuration;
using TradingSystem.RiskManagement.Contracts;
using TradingSystem.RiskManagement.Models;

namespace TradingSystem.RiskManagement.Services;

public sealed class ConfiguredRiskOrderSizingProvider(
    IBotRuntimeConfigurationProvider configProvider,
    IOptions<CentralRiskOptions> options) 
    : IRiskOrderSizingProvider
{
    private readonly CentralRiskOptions _options = options.Value;

    public async Task<RiskOrderSize?> GetAsync(string botName, CancellationToken ct)
    {
        var config = await configProvider.GetAsync(botName, ct);
        if (config is not null && config.Quantity > 0 && config.Leverage > 0)
            return new RiskOrderSize(config.Quantity, config.Leverage, null);

        return _options.BotProfiles.TryGetValue(botName, out var profile)
            ? new RiskOrderSize(profile.Quantity, profile.Leverage, profile.MaximumNotional)
            : null;
    }
}
