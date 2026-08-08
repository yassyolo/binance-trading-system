using Microsoft.Extensions.Options;

namespace TradingSystem.Application.Engine.Configuration;

public sealed class TradingEngineOptionsValidator
    : IValidateOptions<TradingEngineOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        TradingEngineOptions options)
    {
        var errors = new List<string>();

        if (options.ProcessingIdempotencyTtl <= TimeSpan.Zero)
        {
            errors.Add(
                "ProcessingIdempotencyTtl must be positive.");
        }

        if (options.CompletedIdempotencyTtl <
            options.ProcessingIdempotencyTtl)
        {
            errors.Add(
                "CompletedIdempotencyTtl must be greater than or equal to ProcessingIdempotencyTtl.");
        }

        if (options.OperationLockTtl <= TimeSpan.Zero)
        {
            errors.Add(
                "OperationLockTtl must be positive.");
        }

        if (options.MaximumSignalAge < TimeSpan.Zero)
        {
            errors.Add(
                "MaximumSignalAge cannot be negative.");
        }

        if (options.MaximumFutureClockSkew < TimeSpan.Zero)
        {
            errors.Add(
                "MaximumFutureClockSkew cannot be negative.");
        }

        if (options.PostExecutionCompletionRetryCount <= 0)
        {
            errors.Add(
                "PostExecutionCompletionRetryCount must be positive.");
        }

        if (options.PostExecutionCompletionRetryDelay < TimeSpan.Zero)
        {
            errors.Add(
                "PostExecutionCompletionRetryDelay cannot be negative.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}