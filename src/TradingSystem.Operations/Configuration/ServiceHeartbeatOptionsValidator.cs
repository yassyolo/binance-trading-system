using Microsoft.Extensions.Options;

namespace TradingSystem.Operations.Configuration;

public sealed class ServiceHeartbeatOptionsValidator
    : IValidateOptions<ServiceHeartbeatOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        ServiceHeartbeatOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            errors.Add(
                "ServiceName is required.");
        }

        if (options.IntervalSeconds <= 0)
        {
            errors.Add(
                "IntervalSeconds must be positive.");
        }

        if (options.StaleAfterSeconds <= 0)
        {
            errors.Add(
                "StaleAfterSeconds must be positive.");
        }

        if (options.StaleAfterSeconds <
            options.IntervalSeconds)
        {
            errors.Add(
                "StaleAfterSeconds must be greater than or equal to IntervalSeconds.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}