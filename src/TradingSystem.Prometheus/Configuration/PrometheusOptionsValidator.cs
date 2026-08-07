using Microsoft.Extensions.Options;

namespace TradingSystem.Prometheus.Configuration;

public sealed class PrometheusOptionsValidator : IValidateOptions<PrometheusOptions>
{
    public ValidateOptionsResult Validate(string? name, PrometheusOptions options)
    {
        var errors = new List<string>();
        if (options.Port is < 1 or > 65535)
            errors.Add("Prometheus Port must be between 1 and 65535.");
        if (string.IsNullOrWhiteSpace(options.Url) || !options.Url.StartsWith('/'))
            errors.Add("Prometheus Url must start with '/'.");
        if (options.PortfolioRefreshSeconds < 1)
            errors.Add("PortfolioRefreshSeconds must be positive.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
