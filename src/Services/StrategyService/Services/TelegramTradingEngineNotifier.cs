using TradingSystem.Application.Engine;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Signals;

namespace StrategyService.Services;

public sealed class TelegramTradingEngineNotifier(
    TelegramNotificationService telegram,
    ILogger<TelegramTradingEngineNotifier> logger)
    : ITradingEngineNotifier
{
    public Task DecisionMadeAsync(
        TradeSignal signal,
        decimal markPrice,
        StrategyDecision decision,
        CancellationToken cancellationToken)
    {
        if (decision.ShouldOpen)
            return Task.CompletedTask;

        return telegram.SendAsync(
            $"⛔ {signal.BotName} BLOCKED\n" +
            $"Signal: {signal.SignalId}\n" +
            $"Side: {signal.Side}\n" +
            $"Symbol: {signal.Symbol}\n" +
            $"Mark: {markPrice}\n" +
            $"Reason: {decision.Reason}",
            cancellationToken);
    }

    public Task ExecutionCompletedAsync(
        TradeSignal signal,
        TradeExecutionResult result,
        CancellationToken cancellationToken)
    {
        var icon = result.Succeeded ? "✅" : "❌";

        return telegram.SendAsync(
            $"{icon} {signal.BotName} EXECUTION\n" +
            $"Signal: {signal.SignalId}\n" +
            $"Side: {signal.Side}\n" +
            $"Symbol: {signal.Symbol}\n" +
            $"Position: {result.ShortId ?? "-"}\n" +
            $"Result: {result.Reason}",
            cancellationToken);
    }

    public Task ProcessingFailedAsync(
        TradeSignal signal,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Trading engine processing failed. SignalId={SignalId}",
            signal.SignalId);

        return telegram.SendAsync(
            $"🚨 {signal.BotName} ENGINE ERROR\n" +
            $"Signal: {signal.SignalId}\n" +
            $"Side: {signal.Side}\n" +
            $"Symbol: {signal.Symbol}\n" +
            $"Error: {exception.Message}",
            cancellationToken);
    }
}
