using Microsoft.Extensions.Options;

namespace TradingSystem.Application.Engine.Configuration;

public sealed class TradingEngineOptionsValidator : IValidateOptions<TradingEngineOptions>
{
    public ValidateOptionsResult Validate(string? name, TradingEngineOptions options)
    {
        var e = new List<string>();

        if (options.ProcessingIdempotencyTtl <= TimeSpan.Zero)
            e.Add("ProcessingIdempotencyTtl must be positive.");

        if (options.CompletedIdempotencyTtl < options.ProcessingIdempotencyTtl)
            e.Add("CompletedIdempotencyTtl must be greater than or equal to ProcessingIdempotencyTtl.");

        if (options.OperationLockTtl <= TimeSpan.Zero)
            e.Add("OperationLockTtl must be positive.");

        if (options.MaximumSignalAge < TimeSpan.Zero)
            e.Add("MaximumSignalAge cannot be negative.");

        if (options.MaximumFutureClockSkew < TimeSpan.Zero)
            e.Add("MaximumFutureClockSkew cannot be negative.");

        if (options.PostExecutionCompletionRetryCount <= 0)
            e.Add("PostExecutionCompletionRetryCount must be positive.");

        if (options.PostExecutionCompletionRetryDelay < TimeSpan.Zero)
            e.Add("PostExecutionCompletionRetryDelay cannot be negative.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}