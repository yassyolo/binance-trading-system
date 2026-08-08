using Microsoft.Extensions.Options;

namespace TradingSystem.Operations.Configuration;

public sealed class AlertEngineOptionsValidator
    : IValidateOptions<AlertEngineOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        AlertEngineOptions options)
    {
        if (options.PollSeconds <= 0)
        {
            return ValidateOptionsResult.Fail(
                "AlertEngine PollSeconds must be positive.");
        }

        return ValidateOptionsResult.Success;
    }
}