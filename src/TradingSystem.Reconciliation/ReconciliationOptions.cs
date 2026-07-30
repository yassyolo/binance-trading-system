using Microsoft.Extensions.Options;

namespace TradingSystem.Reconciliation;

public sealed record ReconciliationOptions
{
    public const string SectionName = "Reconciliation";

    public bool Enabled { get; init; } = true;

    public int IntervalSeconds { get; init; } = 30;

    public bool AutoHealStaleLocalPositions { get; init; } = true;

    public bool AutoHealProtectiveOrders { get; init; }

    public decimal QuantityTolerance { get; init; } = 0.00000001m;

    public string[] Bots { get; init; } = [];

    public string[] Symbols { get; init; } = [];
}

public sealed class ReconciliationOptionsValidator : IValidateOptions<ReconciliationOptions>
{
    public ValidateOptionsResult Validate(string? name, ReconciliationOptions options)
    {
        var errors = new List<string>();

        if (options.IntervalSeconds is < 5 or > 86_400)
            errors.Add( "Reconciliation:IntervalSeconds must be between 5 and 86400.");

        if (options.QuantityTolerance < 0)
            errors.Add("Reconciliation:QuantityTolerance cannot be negative.");

        if (options.Bots is null || options.Bots.Length == 0 || options.Bots.Any(string.IsNullOrWhiteSpace))
            errors.Add("Reconciliation:Bots must contain at least one non-empty bot name.");

        if (options.Symbols is null || options.Symbols.Length == 0 || options.Symbols.Any(string.IsNullOrWhiteSpace))
            errors.Add("Reconciliation:Symbols must contain at least one non-empty symbol.");

        if (options.Bots is not null && options.Bots.Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Bots.Length)
            errors.Add("Reconciliation:Bots cannot contain duplicates.");

        if (options.Symbols is not null && options.Symbols.Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Symbols.Length)
            errors.Add("Reconciliation:Symbols cannot contain duplicates.");

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}