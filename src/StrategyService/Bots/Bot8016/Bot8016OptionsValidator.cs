using Microsoft.Extensions.Options;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016OptionsValidator : IValidateOptions<Bot8016Options>
{
    public ValidateOptionsResult Validate(string? name, Bot8016Options options)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(options.BotName)) errors.Add("BotName is required.");
        if (string.IsNullOrWhiteSpace(options.Symbol)) errors.Add("Symbol is required.");
        if (options.Quantity <= 0) errors.Add("Quantity must be greater than zero.");
        if (options.Leverage is < 1 or > 125) errors.Add("Leverage must be between 1 and 125.");
        if (options.InitialStopLossFallback <= 0) errors.Add("InitialStopLossFallback must be positive.");
        if (options.TpPercent <= 0) errors.Add("TpPercent must be greater than zero.");
        if (options.Stop3EntryOffset < 0) errors.Add("Stop3EntryOffset cannot be negative.");
        if (options.PositionSideLimit <= 0) errors.Add("PositionSideLimit must be greater than zero.");
        if (options.MinimumSignalCandleRange < 0) errors.Add("MinimumSignalCandleRange cannot be negative.");
        if (options.AlligatorMaxAgeSeconds <= 0) errors.Add("AlligatorMaxAgeSeconds must be greater than zero.");
        if (options.HealingIntervalSeconds <= 0) errors.Add("HealingIntervalSeconds must be greater than zero.");
        if (string.IsNullOrWhiteSpace(options.EntryTimeframe)) errors.Add("EntryTimeframe is required.");
        if (string.IsNullOrWhiteSpace(options.ExitTimeframe)) errors.Add("ExitTimeframe is required.");
        if (string.IsNullOrWhiteSpace(options.AlligatorChannel)) errors.Add("AlligatorChannel is required.");
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
