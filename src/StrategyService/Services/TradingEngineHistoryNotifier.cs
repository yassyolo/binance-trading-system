using TradingSystem.Application.Engine;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Signals;
using TradingSystem.Observability.Environment;
using TradingSystem.Observability.History;

namespace StrategyService.Services;

public sealed class TradingEngineHistoryNotifier(
    TelegramTradingEngineNotifier notifications, 
    TradingStrategyRegistry strategies, 
    IPositionStore positions, 
    ITradingPipelineRecorder history, 
    ITradingEnvironmentProvider environment) : ITradingEngineNotifier
{
    public async Task DecisionMadeAsync(
        TradeSignal signal, 
        decimal markPrice, 
        StrategyDecision decision, 
        CancellationToken cancellationToken)
    {
        var strategy  =  strategies.GetRequired(signal.BotName);

        await history.RecordDecisionAsync(new DecisionHistoryRecord(
            signal.SignalId, 
            signal.BotName, 
            strategy.Metadata.Version, 
            signal.Symbol, 
            signal.Side.ToString(), 
            decision.ShouldOpen ? "Open" : "Block", 
            decision.Reason, 
            environment.EnvironmentName, 
            DateTime.UtcNow, 
            markPrice, 
            new Dictionary<string,  object?>
            {
                ["positions_to_close"]  =  decision.PositionsToClose
            }),  cancellationToken);

        await notifications.DecisionMadeAsync(
            signal, 
            markPrice, 
            decision, 
            cancellationToken);
    }

    public async Task ExecutionCompletedAsync(
        TradeSignal signal, 
        TradeExecutionResult result, 
        CancellationToken cancellationToken)
    {
        if (result.Succeeded  &&  !string.IsNullOrWhiteSpace(result.ShortId))
        {
            var position  =  await positions.GetAsync(
                signal.BotName, 
                result.ShortId, 
                cancellationToken);

            if (position is not null)
            {
                var strategy  =  strategies.GetRequired(signal.BotName);

                await history.UpsertPositionAsync(new PositionHistoryRecord(
                    position.ShortId, 
                    signal.SignalId, 
                    position.BotName, 
                    strategy.Metadata.Version, 
                    position.Symbol, 
                    position.Side.ToString(), 
                    position.Source, 
                    environment.EnvironmentName, 
                    position.Status.ToString(), 
                    position.Quantity, 
                    position.EntryPrice, 
                    position.TpPrice, 
                    position.ParentFilledAtUtc ?? position.CreatedAtUtc, 
                    position.ClosedAtUtc, 
                    null, 
                    null, 
                    position.CloseStatus),  cancellationToken);
            }
        }

        await notifications.ExecutionCompletedAsync(
            signal, 
            result, 
            cancellationToken);
    }

    public Task ProcessingFailedAsync(
        TradeSignal signal, 
        Exception exception, 
        CancellationToken cancellationToken)
         =>  notifications.ProcessingFailedAsync(
            signal, 
            exception, 
            cancellationToken);
}
