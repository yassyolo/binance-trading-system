using Microsoft.Extensions.Options;

namespace TradingSystem.Indicators.Alligator.Configuration;

public sealed class AlligatorOptionsValidator : IValidateOptions<AlligatorOptions>
{
    public ValidateOptionsResult Validate(string? name, AlligatorOptions options)
    {
        var e = new List<string>();

        if (options.Symbols is null ||
            options.Symbols.Length == 0 ||
            options.Symbols.Any(string.IsNullOrWhiteSpace))
            e.Add("Alligator symbols are required and cannot contain empty values.");

        if (options.Intervals is null ||
            options.Intervals.Length == 0 ||
            options.Intervals.Any(string.IsNullOrWhiteSpace))
            e.Add("Alligator intervals are required and cannot contain empty values.");

        if (options.SmaLength <= 0)
            e.Add("Alligator SmaLength must be positive.");

        if (options.JawLength <= 0)
            e.Add("Alligator JawLength must be positive.");

        if (options.TeethLength <= 0)
            e.Add("Alligator TeethLength must be positive.");

        if (options.LipsLength <= 0)
            e.Add("Alligator LipsLength must be positive.");

        if (options.HistoryLimit <= 0)
            e.Add("Alligator HistoryLimit must be positive.");

        if (options.HistoryLimit < options.SmaLength)
            e.Add("Alligator HistoryLimit must be greater than or equal to SmaLength.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}