using Microsoft.Extensions.Options;

namespace StrategyService.Runtime;

public sealed class BotRuntimeOptionsValidator : IValidateOptions<BotRuntimeOptions>
{
    public ValidateOptionsResult Validate(string? name, BotRuntimeOptions options)
    {
        var errors = new List<string>();
        if (options.CommandPollSeconds <= 0) errors.Add("CommandPollSeconds must be positive.");
        if (options.CommandBatchSize <= 0) errors.Add("CommandBatchSize must be positive.");
        if (options.CommandProcessingTimeoutSeconds < 10) errors.Add("CommandProcessingTimeoutSeconds must be at least 10.");
        if (options.ConfigurationRefreshSeconds <= 0) errors.Add("ConfigurationRefreshSeconds must be positive.");
        if (options.MaximumCommandAttempts <= 0) errors.Add("MaximumCommandAttempts must be positive.");
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
