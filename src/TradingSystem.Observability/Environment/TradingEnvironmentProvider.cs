using Microsoft.Extensions.Options;
using TradingSystem.Observability.Configuration;

namespace TradingSystem.Observability.Environment;

public sealed class TradingEnvironmentProvider(
    IOptions<TradingEnvironmentOptions> options)
    : ITradingEnvironmentProvider
{
    private readonly TradingEnvironmentOptions _options = options.Value;

    public string EnvironmentName
    {
        get
        {
            var environmentOverride = System.Environment.GetEnvironmentVariable("TRADING_ENVIRONMENT")?.Trim();

            if (!string.IsNullOrWhiteSpace(environmentOverride))
                return environmentOverride;

            return string.IsNullOrWhiteSpace(_options.EnvironmentName) ? "Paper" : _options.EnvironmentName.Trim();
        }
    }
}
