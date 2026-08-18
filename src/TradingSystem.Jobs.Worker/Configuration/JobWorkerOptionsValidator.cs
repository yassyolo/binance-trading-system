using Microsoft.Extensions.Options;

namespace TradingSystem.Jobs.Worker.Configuration;

public sealed class JobWorkerOptionsValidator : IValidateOptions<JobWorkerOptions>
{
    public ValidateOptionsResult Validate(string? name, JobWorkerOptions options)
    {
        var e = new List<string>();

        if (options.PollSeconds <= 0)
            e.Add("PollSeconds must be positive.");

        if (options.BatchSize <= 0)
            e.Add("BatchSize must be positive.");

        if (options.ProcessingTimeoutMinutes <= 0)
            e.Add("ProcessingTimeoutMinutes must be positive.");

        if (options.MaximumAttempts <= 0)
            e.Add("MaximumAttempts must be positive.");

        if (string.IsNullOrWhiteSpace(options.Interval))
            e.Add("Interval is required.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}