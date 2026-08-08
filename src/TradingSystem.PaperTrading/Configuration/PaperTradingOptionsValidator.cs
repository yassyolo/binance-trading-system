using Microsoft.Extensions.Options;

namespace TradingSystem.PaperTrading.Configuration;

public sealed class PaperTradingOptionsValidator : IValidateOptions<PaperTradingOptions>
{
    public ValidateOptionsResult Validate(string? name, PaperTradingOptions options)
    {
        var e = new List<string>();

        if (options.InitialBalance <= 0)
            e.Add("InitialBalance must be positive.");

        if (options.CommissionPercent < 0)
            e.Add("CommissionPercent cannot be negative.");

        if (options.SlippagePercent < 0)
            e.Add("SlippagePercent cannot be negative.");

        if (options.DefaultTakeProfitPercent <= 0)
            e.Add("DefaultTakeProfitPercent must be positive.");

        if (options.DefaultStopLossPercent <= 0)
            e.Add("DefaultStopLossPercent must be positive.");

        if (options.PricePollMilliseconds <= 0)
            e.Add("PricePollMilliseconds must be positive.");

        if (options.MaximumOpenPositions <= 0)
            e.Add("MaximumOpenPositions must be positive.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}