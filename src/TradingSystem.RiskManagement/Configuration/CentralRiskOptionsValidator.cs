using Microsoft.Extensions.Options;

namespace TradingSystem.RiskManagement.Configuration;

public sealed class CentralRiskOptionsValidator : IValidateOptions<CentralRiskOptions>
{
    public ValidateOptionsResult Validate(string? name, CentralRiskOptions options)
    {
        var e = new List<string>();

        if (options.MaximumOpenPositions <= 0)
            e.Add("MaximumOpenPositions must be positive.");
        
        if (options.MaximumOpenPositionsPerBot <= 0)
            e.Add("MaximumOpenPositionsPerBot must be positive.");
        
        if (options.MaximumOpenPositionsPerSymbol <= 0)
            e.Add("MaximumOpenPositionsPerSymbol must be positive.");
        
        if (options.MaximumEstimatedNotional <= 0)
            e.Add("MaximumEstimatedNotional must be positive.");     
        
        if (options.MaximumDailyLoss < 0)
            e.Add("MaximumDailyLoss cannot be negative.");
        
        if (options.MaximumDailyDrawdownPercent is < 0 or > 100)
            e.Add("MaximumDailyDrawdownPercent must be between 0 and 100.");
        
        if (options.MaximumConsecutiveLosses < 0)
            e.Add("MaximumConsecutiveLosses cannot be negative.");
       
        if (options.AdmissionReservationSeconds <= 0)
            e.Add("AdmissionReservationSeconds must be positive.");

        foreach (var (botName, profile) in options.BotProfiles)
        {
            if (string.IsNullOrWhiteSpace(botName))
                e.Add("Risk bot profile name cannot be empty.");
            
            if (profile.Quantity <= 0)
                e.Add($"Risk profile '{botName}' Quantity must be positive.");
            
            if (profile.Leverage is < 1 or > 125)
                e.Add($"Risk profile '{botName}' Leverage must be between 1 and 125.");
            
            if (profile.MaximumNotional is <= 0)
                e.Add($"Risk profile '{botName}' MaximumNotional must be positive when configured.");
        }

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}
