using Microsoft.Extensions.Options;

namespace TradingSystem.RiskManagement;

public sealed class CentralRiskOptionsValidator : IValidateOptions<CentralRiskOptions>
{
    public ValidateOptionsResult Validate(string? name, CentralRiskOptions options)
    {
        var errors = new List<string>();

        if (options.MaximumOpenPositions <= 0)
            errors.Add("MaximumOpenPositions must be positive.");
        if (options.MaximumOpenPositionsPerBot <= 0)
            errors.Add("MaximumOpenPositionsPerBot must be positive.");
        if (options.MaximumOpenPositionsPerSymbol <= 0)
            errors.Add("MaximumOpenPositionsPerSymbol must be positive.");
        if (options.MaximumEstimatedNotional <= 0)
            errors.Add("MaximumEstimatedNotional must be positive.");
        if (options.MaximumGrossNotionalPerSymbol < 0)
            errors.Add("MaximumGrossNotionalPerSymbol cannot be negative.");
        if (options.MaximumAbsoluteNetNotionalPerSymbol < 0)
            errors.Add("MaximumAbsoluteNetNotionalPerSymbol cannot be negative.");
        if (options.MaximumDailyLoss < 0)
            errors.Add("MaximumDailyLoss cannot be negative.");
        if (options.MaximumDailyDrawdownPercent is < 0 or > 100)
            errors.Add("MaximumDailyDrawdownPercent must be between 0 and 100.");
        if (options.MaximumConsecutiveLosses < 0)
            errors.Add("MaximumConsecutiveLosses cannot be negative.");
        if (options.AdmissionReservationSeconds <= 0)
            errors.Add("AdmissionReservationSeconds must be positive.");

        foreach (var (botName, profile) in options.BotProfiles)
        {
            if (string.IsNullOrWhiteSpace(botName))
                errors.Add("Risk bot profile name cannot be empty.");
            if (profile.Quantity <= 0)
                errors.Add($"Risk profile '{botName}' Quantity must be positive.");
            if (profile.Leverage is < 1 or > 125)
                errors.Add($"Risk profile '{botName}' Leverage must be between 1 and 125.");
            if (profile.MaximumNotional is <= 0)
                errors.Add($"Risk profile '{botName}' MaximumNotional must be positive when configured.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
