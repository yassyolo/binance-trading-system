using Microsoft.Extensions.Options;

namespace MarketDataService.Configuration;

public sealed class MarketDataOptionsValidator : IValidateOptions<MarketDataOptions>
{
    public ValidateOptionsResult Validate(string? name, MarketDataOptions options)
    {
        var e = new List<string>();

        if (options.Symbols is null || !options.Symbols.Any(x => !string.IsNullOrWhiteSpace(x)))
            e.Add("At least one market data symbol is required.");

        if (options.Intervals is null || !options.Intervals.Any(x => !string.IsNullOrWhiteSpace(x)))
            e.Add("At least one market data interval is required.");

        if (!Uri.TryCreate(options.BinanceWebSocketBaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeWs &&
             uri.Scheme != Uri.UriSchemeWss))
        {
            e.Add("BinanceWebSocketBaseUrl must be a valid ws:// or wss:// URL.");
        }

        if (options.ReconnectDelaySeconds <= 0)
            e.Add("ReconnectDelaySeconds must be positive.");

        if (options.MaximumReconnectDelaySeconds < options.ReconnectDelaySeconds)
            e.Add("MaximumReconnectDelaySeconds must be greater than or equal to ReconnectDelaySeconds.");

        if (options.KeepAliveIntervalSeconds <= 0)           
            e.Add("KeepAliveIntervalSeconds must be positive.");

        if (options.ReceiveBufferSizeBytes < 1_024)
            e.Add("ReceiveBufferSizeBytes must be at least 1024 bytes.");

        if (options.LatestKlineTtlSeconds <= 0)
            e.Add("LatestKlineTtlSeconds must be positive.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}