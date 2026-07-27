using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine;
using TradingSystem.Application.Positions;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016EntryCoordinator(
    IOptions<Bot8016Options> options,
    Bot8016EntrySignalEvaluator evaluator,
    Bot8016OrderExecutionService execution,
    IPositionStore positionStore,
    ITradingOperationLockProvider operationLocks,
    TelegramNotificationService telegram,
    ILogger<Bot8016EntryCoordinator> logger)
{
    private readonly Bot8016Options _options = options.Value;

    public async Task ProcessAsync(Bot8016Candle candle, Bot8016IndicatorSnapshot indicator, CancellationToken cancellationToken)
    {
        var signal = evaluator.Evaluate(candle, indicator);
        if (signal is null) return;

        await using var operationLock = await operationLocks.TryAcquireAsync(
            _options.BotName,
            _options.Symbol,
            signal.Side,
            TimeSpan.FromSeconds(30),
            cancellationToken);

        if (operationLock is null)
        {
            logger.LogInformation("BOT8016 entry skipped because another entry is in progress. Side = {Side}", signal.Side);
            return;
        }

        var positions = await positionStore.GetAllAsync(_options.BotName, cancellationToken);
        var sameSideCount = positions.Count(x => !x.Closed && x.Side == signal.Side);
        if (sameSideCount >= _options.PositionSideLimit)
        {
            logger.LogInformation(
                "BOT8016 signal blocked by side limit. Side = {Side}, Count = {Count}, Limit = {Limit}",
                signal.Side, sameSideCount, _options.PositionSideLimit);
            return;
        }

        var position = await execution.OpenAsync(signal, cancellationToken);
        await positionStore.SaveAsync(position, cancellationToken);

        await telegram.SendAsync(
            $"🚀 {_options.BotName} opened {position.Side}\n" +
            $"ID: {position.ShortId}\nEntry: {position.EntryPrice}\nSL: {position.SlPrice}\nTP: {position.TpPrice}\n" +
            $"Signal H/L: {position.SignalCandleHigh}/{position.SignalCandleLow}",
            cancellationToken);
    }
}
