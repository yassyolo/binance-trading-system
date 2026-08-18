using Microsoft.Extensions.Options;

namespace TradingSystem.Jobs.Worker.Configuration;

public sealed class HistoricalDataIngestionOptionsValidator : IValidateOptions<HistoricalDataIngestionOptions>
{
    public ValidateOptionsResult Validate(string? name, HistoricalDataIngestionOptions options)
    {
        var e = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Environment))
            e.Add("Environment is required.");

        if (options.Symbols is null ||
            options.Symbols.Length == 0 ||
            options.Symbols.Any(string.IsNullOrWhiteSpace))
            e.Add("At least one valid historical ingestion symbol is required.");

        if (options.Intervals is null ||
            options.Intervals.Length == 0 ||
            options.Intervals.Any(string.IsNullOrWhiteSpace))
            e.Add("At least one valid historical ingestion interval is required.");

        if (options.PollMinutes <= 0)
            e.Add("PollMinutes must be positive.");

        if (options.InitialLookbackDays <= 0)
            e.Add("InitialLookbackDays must be positive.");

        if (options.OverlapCandles < 0)
            e.Add("OverlapCandles cannot be negative.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}