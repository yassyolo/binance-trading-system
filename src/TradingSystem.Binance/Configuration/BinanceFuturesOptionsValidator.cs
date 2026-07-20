using Microsoft.Extensions.Options;

namespace TradingSystem.Binance.Configuration;

public sealed class BinanceFuturesOptionsValidator : IValidateOptions<BinanceFuturesOptions>
{
    public ValidateOptionsResult Validate(string? name,  BinanceFuturesOptions options)
    {
        var errors  =  new List<string>();
        if (!Uri.TryCreate(options.BaseUrl,  UriKind.Absolute,  out _)) errors.Add("BinanceFutures:BaseUrl must be an absolute URI.");
        if (options.ReceiveWindow is < 1 or > 60000) errors.Add("BinanceFutures:ReceiveWindow must be between 1 and 60000.");
        if (string.IsNullOrWhiteSpace(options.ApiKey)) errors.Add("BinanceFutures:ApiKey is required for signed operations.");
        if (string.IsNullOrWhiteSpace(options.SecretKey)) errors.Add("BinanceFutures:SecretKey is required for signed operations.");
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
