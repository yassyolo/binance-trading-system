using Microsoft.Extensions.Options;

namespace AlligatorIndicatorService.Configuration;

public sealed class AlligatorOptionsValidator : IValidateOptions<AlligatorOptions>
{
    public ValidateOptionsResult Validate(string? name, AlligatorOptions options)
    {
        var errors = new List<string>();

        if (options.Symbols.Length == 0)
            errors.Add("At least one symbol is required.");

        if (options.Intervals.Length == 0)
            errors.Add("At least one interval is required.");

        if (options.HistoryLimit < options.SmaLength)
            errors.Add("HistoryLimit must be greater than or equal to SmaLength.");

        if (options.SmaLength <= 0)
            errors.Add("SmaLength must be greater than zero.");

        if (options.JawLength <= 0 || options.TeethLength <= 0 || options.LipsLength <= 0)
            errors.Add("All Alligator lengths must be greater than zero.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
