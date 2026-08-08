using Microsoft.Extensions.Options;

namespace TradingSystem.Prometheus.Configuration;

public sealed class PrometheusOptionsValidator : IValidateOptions<PrometheusOptions>
{
    public ValidateOptionsResult Validate(string? name, PrometheusOptions options)
    {
        var e = new List<string>();
       
        if (options.Port is < 1 or > 65535)
            e.Add("Prometheus Port must be between 1 and 65535.");
       
        if (string.IsNullOrWhiteSpace(options.Url) || !options.Url.StartsWith('/'))
            e.Add("Prometheus Url must start with '/'.");
        
        if (options.PortfolioRefreshSeconds < 1)
            e.Add("PortfolioRefreshSeconds must be positive.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}
