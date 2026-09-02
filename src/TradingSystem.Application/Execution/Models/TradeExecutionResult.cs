namespace TradingSystem.Application.Execution.Models;

public sealed record TradeExecutionResult
{
    private TradeExecutionResult(bool succeeded,  string? shortId,  string reason,  Exception? exception)
    {
        Succeeded = succeeded;
        ShortId = shortId;
        Reason = reason;
        Exception = exception;
    }

    public bool Succeeded { get; }
    public string? ShortId { get; }
    public string Reason { get; }
    public Exception? Exception { get; }

    public static TradeExecutionResult Success(string shortId,  string reason = "Position opened")  =>  new(true,  shortId,  reason,  null);
    public static TradeExecutionResult Failure(string reason,  Exception? exception = null)  =>  new(false,  null,  reason,  exception);
}
