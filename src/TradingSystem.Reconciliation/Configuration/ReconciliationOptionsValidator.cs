using Microsoft.Extensions.Options;

namespace TradingSystem.Reconciliation.Configuration;

public sealed class ReconciliationOptionsValidator : IValidateOptions<ReconciliationOptions>
{
    public ValidateOptionsResult Validate(string? name, ReconciliationOptions options)
    {
        var e = new List<string>();

        if (options.IntervalSeconds is < 5 or > 86_400)
            e.Add("Reconciliation:IntervalSeconds must be between 5 and 86400.");

        if (options.QuantityTolerance < 0)
            e.Add("Reconciliation:QuantityTolerance cannot be negative.");

        if (options.Bots is null || options.Bots.Length == 0 || options.Bots.Any(string.IsNullOrWhiteSpace))
            e.Add("Reconciliation:Bots must contain at least one non-empty bot name.");

        if (options.Symbols is null || options.Symbols.Length == 0 || options.Symbols.Any(string.IsNullOrWhiteSpace))
            e.Add("Reconciliation:Symbols must contain at least one non-empty symbol.");

        if (options.Bots is not null && options.Bots.Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Bots.Length)
            e.Add("Reconciliation:Bots cannot contain duplicates.");

        if (options.Symbols is not null && options.Symbols.Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Symbols.Length)
            e.Add("Reconciliation:Symbols cannot contain duplicates.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}
