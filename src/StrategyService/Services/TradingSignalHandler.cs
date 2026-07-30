using TradingSystem.Application.Engine;
using TradingSystem.Application.Execution;
using TradingSystem.Domain.Signals;

namespace StrategyService.Services;

public sealed class TradingSignalHandler(
    TradingEngine engine, 
    ILogger<TradingSignalHandler> logger):
    ITradingSignalHandler
{
    public async Task<bool> HandleAsync(TradeSignal signal, CancellationToken ct)
    {
        var r = await engine.ProcessSignalAsync(signal, ct);
        
        logger.LogInformation("Signal processed. Id = {Id} Bot = {Bot} Opened = {Opened} Reason = {Reason}", signal.SignalId, signal.BotName, r.OpenedPosition, r.Reason);
        
        return r.Succeeded;
    }
}
