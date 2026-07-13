using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Models;
using StrategyService.Strategies.Bot8016;
using TradingSystem.Application.Positions;

namespace StrategyService.Services;

public sealed class Bot8016EntryCoordinator(
    IOptions<Bot8016Options> options,
    Bot8016EntrySignalEvaluator evaluator,
    Bot8016OrderExecutionService execution,
    IPositionStore positionStore,
    TelegramNotificationService telegram,
    ILogger<Bot8016EntryCoordinator> logger)
{
    private readonly Bot8016Options _options = options.Value;

    public async Task ProcessAsync(
        Bot8016Candle candle,
        Bot8016IndicatorSnapshot indicator,
        CancellationToken cancellationToken)
    {
        var signal = evaluator.Evaluate(candle, indicator);
        if (signal is null)
            return;

        var positions = await positionStore.GetAllAsync(
            _options.BotName,
            cancellationToken);

        var sameSideCount = positions.Count(x =>
            !x.Closed && x.Side == signal.Side);

        if (sameSideCount >= _options.PositionSideLimit)
        {
            logger.LogInformation(
                "BOT8016 signal blocked by side limit. Side={Side}, Count={Count}, Limit={Limit}",
                signal.Side,
                sameSideCount,
                _options.PositionSideLimit);
            return;
        }

        var position = await execution.OpenAsync(
            signal,
            cancellationToken);

        await positionStore.SaveAsync(
            position,
            cancellationToken);

        await telegram.SendAsync(
            $"🚀 {_options.BotName} opened {position.Side}\n" +
            $"ID: {position.ShortId}\n" +
            $"Entry: {position.EntryPrice}\n" +
            $"SL: {position.SlPrice}\n" +
            $"TP: {position.TpPrice}\n" +
            $"Signal H/L: {position.SignalCandleHigh}/{position.SignalCandleLow}",
            cancellationToken);
    }
}
