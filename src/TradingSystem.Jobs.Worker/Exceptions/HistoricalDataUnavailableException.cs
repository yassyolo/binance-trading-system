namespace TradingSystem.Jobs.Worker.Exceptions;

public sealed class HistoricalDataUnavailableException : Exception
{
    public HistoricalDataUnavailableException(string message) : base(message)
    {}

    public HistoricalDataUnavailableException(string message, Exception innerEx) : base(message, innerEx)
    {}
}
