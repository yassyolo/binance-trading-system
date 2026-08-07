using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;

namespace TradingSystem.Application.Execution;

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
