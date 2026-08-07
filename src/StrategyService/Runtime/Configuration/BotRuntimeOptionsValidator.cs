using Microsoft.Extensions.Options;

namespace StrategyService.Runtime.Configuration;

public sealed class BotRuntimeOptionsValidator : IValidateOptions<BotRuntimeOptions>
{
    public ValidateOptionsResult Validate(string? name, BotRuntimeOptions options)
    {
        var e = new List<string>();
        
        if (options.CommandPollSeconds <= 0) 
            e.Add("CommandPollSeconds must be positive.");
        
        if (options.CommandBatchSize <= 0) 
            e.Add("CommandBatchSize must be positive.");
        
        if (options.CommandProcessingTimeoutSeconds < 10)
            e.Add("CommandProcessingTimeoutSeconds must be at least 10.");
        
        if (options.ConfigurationRefreshSeconds <= 0) 
            e.Add("ConfigurationRefreshSeconds must be positive.");
        
        if (options.MaximumCommandAttempts <= 0)
            e.Add("MaximumCommandAttempts must be positive.");
        
        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}
