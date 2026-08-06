namespace TradingSystem.Jobs.Worker.Execution;

/// <summary>
/// Indicates that a requested job cannot be executed because the required
/// historical dataset is absent, incomplete, or has unresolved gaps.
/// Retrying the same immutable request cannot fix this condition.
/// </summary>
public sealed class HistoricalDataUnavailableException : Exception
{
    public HistoricalDataUnavailableException(string message)
        : base(message)
    {
    }

    public HistoricalDataUnavailableException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
