using Microsoft.Extensions.Options;

namespace TradingSystem.Operations.Configuration;

public sealed class ServiceHeartbeatOptionsValidator : IValidateOptions<ServiceHeartbeatOptions>
{
    public ValidateOptionsResult Validate(string? name, ServiceHeartbeatOptions options)
    {
        var e = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ServiceName))
            e.Add("ServiceName is required.");

        if (options.IntervalSeconds <= 0)
            e.Add("IntervalSeconds must be positive.");

        if (options.StaleAfterSeconds <= 0)
            e.Add("StaleAfterSeconds must be positive.");

        if (options.StaleAfterSeconds < options.IntervalSeconds)
            e.Add("StaleAfterSeconds must be greater than or equal to IntervalSeconds.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}