namespace TradingSystem.Application.Execution;

public sealed record TradingSignalExecutionContext(
    string SignalId,
    string StrategyVersion,
    string? Source);

public interface ITradingSignalContextAccessor
{
    TradingSignalExecutionContext? Current { get; }

    IDisposable Push(TradingSignalExecutionContext context);
}

public sealed class TradingSignalContextAccessor : ITradingSignalContextAccessor
{
    private static readonly AsyncLocal<TradingSignalExecutionContext?> CurrentContext = new();

    public TradingSignalExecutionContext? Current => CurrentContext.Value;

    public IDisposable Push(TradingSignalExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var previous = CurrentContext.Value;
        CurrentContext.Value = context;

        return new Scope(previous);
    }

    private sealed class Scope(TradingSignalExecutionContext? previous) : IDisposable
    {
        private TradingSignalExecutionContext? _previous = previous;

        public void Dispose()
        {
            CurrentContext.Value = _previous;
            _previous = null;
        }
    }
}
