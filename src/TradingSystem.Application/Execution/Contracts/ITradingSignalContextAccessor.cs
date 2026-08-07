using TradingSystem.Application.Execution.Models;

namespace TradingSystem.Application.Execution.Contracts;

public interface ITradingSignalContextAccessor
{
    TradingSignalExecutionContext? Current { get; }

    IDisposable Push(TradingSignalExecutionContext context);
}
