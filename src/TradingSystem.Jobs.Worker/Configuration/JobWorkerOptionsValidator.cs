using Microsoft.Extensions.Options;

namespace TradingSystem.Jobs.Worker.Configuration;

public sealed class JobWorkerOptionsValidator
    : IValidateOptions<JobWorkerOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        JobWorkerOptions options)
    {
        var errors = new List<string>();

        if (options.PollSeconds <= 0)
        {
            errors.Add(
                "PollSeconds must be positive.");
        }

        if (options.BatchSize <= 0)
        {
            errors.Add(
                "BatchSize must be positive.");
        }

        if (options.ProcessingTimeoutMinutes <= 0)
        {
            errors.Add(
                "ProcessingTimeoutMinutes must be positive.");
        }

        if (options.MaximumAttempts <= 0)
        {
            errors.Add(
                "MaximumAttempts must be positive.");
        }

        if (string.IsNullOrWhiteSpace(options.Interval))
        {
            errors.Add(
                "Interval is required.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}