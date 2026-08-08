using Microsoft.Extensions.Options;

namespace TradingSystem.PaperTrading.Configuration;

public sealed class PaperTradingOptionsValidator
    : IValidateOptions<PaperTradingOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        PaperTradingOptions options)
    {
        var errors = new List<string>();

        if (options.InitialBalance <= 0)
        {
            errors.Add(
                "InitialBalance must be positive.");
        }

        if (options.CommissionPercent < 0)
        {
            errors.Add(
                "CommissionPercent cannot be negative.");
        }

        if (options.SlippagePercent < 0)
        {
            errors.Add(
                "SlippagePercent cannot be negative.");
        }

        if (options.DefaultTakeProfitPercent <= 0)
        {
            errors.Add(
                "DefaultTakeProfitPercent must be positive.");
        }

        if (options.DefaultStopLossPercent <= 0)
        {
            errors.Add(
                "DefaultStopLossPercent must be positive.");
        }

        if (options.PricePollMilliseconds <= 0)
        {
            errors.Add(
                "PricePollMilliseconds must be positive.");
        }

        if (options.MaximumOpenPositions <= 0)
        {
            errors.Add(
                "MaximumOpenPositions must be positive.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}