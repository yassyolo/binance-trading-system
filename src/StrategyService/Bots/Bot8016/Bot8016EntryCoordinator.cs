using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8016.Configuration;
using StrategyService.Bots.Bot8016.Models;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.BotRuntime.Runtime.Contracts;
using TradingSystem.PaperTrading.Contracts;
using TradingSystem.PaperTrading.Executor;
using TradingSystem.PaperTrading.Models.Enums;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016EntryCoordinator(
    IOptions<Bot8016Options> options,
    Bot8016EntrySignalEvaluator evaluator,
    Bot8016OrderExecutionService liveExecution,
    IPositionStore livePositionStore,
    IPaperTradingStore paperTradingStore,
    PaperTradeExecutor paperExecutor,
    IBotRuntimeConfigurationProvider runtimeConfigurations,
    IBotRuntimeStateProvider runtimeStates,
    ITradingOperationLockProvider operationLocks,
    TelegramNotificationService telegram,
    ILogger<Bot8016EntryCoordinator> logger)
{
    private readonly Bot8016Options _options = options.Value;

    public async Task ProcessAsync(
        Bot8016Candle candle,
        Bot8016IndicatorSnapshot indicator,
        CancellationToken cancellationToken)
    {
        var runtimeState = await runtimeStates.GetRequiredAsync(_options.BotName, cancellationToken);
        if (!runtimeState.AcceptsNewSignals)
        {
            logger.LogInformation(
                "BOT8016 entry blocked by runtime state. Status = {Status}, ExecutionEnabled = {ExecutionEnabled}, Reason = {Reason}",
                runtimeState.Status, runtimeState.ExecutionEnabled, runtimeState.Reason);
            return;
        }

        var runtimeConfiguration = await runtimeConfigurations.GetAsync(_options.BotName, cancellationToken);
        if (runtimeConfiguration is null)
        {
            logger.LogWarning("BOT8016 entry blocked because runtime configuration was not found.");
            return;
        }

        if (!runtimeConfiguration.Symbol.Equals(candle.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "BOT8016 entry blocked because the candle symbol does not match runtime configuration. CandleSymbol = {CandleSymbol}, ConfiguredSymbol = {ConfiguredSymbol}",
                candle.Symbol, runtimeConfiguration.Symbol);
            return;
        }

        logger.LogInformation(
            "BOT8016 evaluating entry. Environment = {Environment}, Symbol = {Symbol}, Interval = {Interval}, Open = {Open}, High = {High}, Low = {Low}, Close = {Close}, Jaw = {Jaw}, Teeth = {Teeth}, Lips = {Lips}, SMA200 = {Sma200}",
            runtimeConfiguration.Environment, candle.Symbol, candle.Interval, candle.Open, candle.High, candle.Low, candle.Close,
            indicator.Jaw, indicator.Teeth, indicator.Lips, indicator.Sma200);

        var signal = evaluator.Evaluate(candle, indicator);
        if (signal is null)
        {
            logger.LogInformation(
                "BOT8016 produced no entry signal. CandleRange = {CandleRange}, MinimumRange = {MinimumRange}, UseMa200Filter = {UseMa200Filter}, EnableLong = {EnableLong}, EnableShort = {EnableShort}",
                candle.High - candle.Low, _options.MinimumSignalCandleRange, _options.UseMa200Filter,
                _options.EnableLong, _options.EnableShort);
            return;
        }

        logger.LogInformation(
            "BOT8016 entry signal generated. Side = {Side}, Reason = {Reason}, CandleCloseTime = {CandleCloseTime}, Environment = {Environment}",
            signal.Side, signal.Reason, candle.CloseTime, runtimeConfiguration.Environment);

        await using var operationLock = await operationLocks.TryAcquireAsync(
            _options.BotName,
            runtimeConfiguration.Symbol,
            signal.Side,
            TimeSpan.FromSeconds(30),
            cancellationToken);

        if (operationLock is null)
        {
            logger.LogInformation("BOT8016 entry skipped because another entry is in progress. Side = {Side}", signal.Side);
            return;
        }

        if (IsPaper(runtimeConfiguration))
        {
            await ProcessPaperEntryAsync(signal, runtimeConfiguration, cancellationToken);
            return;
        }

        await ProcessLiveEntryAsync(signal, runtimeConfiguration, cancellationToken);
    }

    private async Task ProcessPaperEntryAsync(Bot8016EntrySignal signal, BotRuntimeConfiguration runtimeConfiguration, CancellationToken cancellationToken)
    {
        var openPaperPositions = await paperTradingStore.QueryAsync(
            _options.BotName,
            runtimeConfiguration.Symbol,
            PaperPositionStatus.Open,
            0,
            500,
            cancellationToken);

        var sameSideCount = openPaperPositions.Count(position => position.Side == signal.Side);
        var sideLimit = runtimeConfiguration.OrderSideLimit ?? _options.PositionSideLimit;

        if (sameSideCount >= sideLimit)
        {
            logger.LogInformation("BOT8016 paper signal blocked by side limit. Side = {Side}, Count = {Count}, Limit = {Limit}", signal.Side, sameSideCount, sideLimit);
            
            return;
        }

        var result = await paperExecutor.OpenAsync(
            _options.BotName,
            runtimeConfiguration.Symbol,
            signal.Side,
            "alligator",
            cancellationToken);

        if (!result.Succeeded)
        {
            logger.LogWarning(
                result.Exception,
                "BOT8016 paper execution failed. Side = {Side}, Reason = {Reason}",
                signal.Side, result.Reason);
            return;
        }

        logger.LogInformation(
            "BOT8016 paper position opened. ShortId = {ShortId}, Side = {Side}, Reason = {Reason}",
            result.ShortId, signal.Side, result.Reason);

        try
        {
            await telegram.SendAsync(
                $"🧪 {_options.BotName} PAPER opened {signal.Side}" +
                $"ID: {result.ShortId}" +
                $"Symbol: {runtimeConfiguration.Symbol}" +
                $"Signal reason: {signal.Reason}",
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "BOT8016 paper position was opened, but Telegram notification failed. ShortId = {ShortId}",
                result.ShortId);
        }
    }

    private async Task ProcessLiveEntryAsync(
        Bot8016EntrySignal signal,
        BotRuntimeConfiguration runtimeConfiguration,
        CancellationToken cancellationToken)
    {
        var positions = await livePositionStore.GetAllAsync(_options.BotName, cancellationToken);
        var sameSideCount = positions.Count(position => !position.Closed && position.Side == signal.Side);
        var sideLimit = runtimeConfiguration.OrderSideLimit ?? _options.PositionSideLimit;

        if (sameSideCount >= sideLimit)
        {
            logger.LogInformation(
                "BOT8016 live signal blocked by side limit. Side = {Side}, Count = {Count}, Limit = {Limit}",
                signal.Side, sameSideCount, sideLimit);
            return;
        }

        logger.LogWarning(
            "BOT8016 is invoking Binance execution. Environment = {Environment}, Bot = {BotName}, Symbol = {Symbol}, Side = {Side}",
            runtimeConfiguration.Environment, _options.BotName, runtimeConfiguration.Symbol, signal.Side);

        var position = await liveExecution.OpenAsync(signal, cancellationToken);
        await livePositionStore.SaveAsync(position, cancellationToken);

        logger.LogInformation(
            "BOT8016 live position opened and saved. ShortId = {ShortId}, Side = {Side}, EntryPrice = {EntryPrice}, StopLoss = {StopLoss}, TakeProfit = {TakeProfit}",
            position.ShortId, position.Side, position.EntryPrice, position.SlPrice, position.TpPrice);

        try
        {
            await telegram.SendAsync(
                $"🚀 {_options.BotName} opened {position.Side}" +
                $"ID: {position.ShortId} Entry: { position.EntryPrice} SL: { position.SlPrice} TP: { position.TpPrice}" +
                $"Signal H/L: {position.SignalCandleHigh}/{position.SignalCandleLow}",
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "BOT8016 live position was opened and saved, but Telegram notification failed. ShortId = {ShortId}",
                position.ShortId);
        }
    }

    private static bool IsPaper(BotRuntimeConfiguration configuration) =>
        configuration.Environment.Equals("Paper", StringComparison.OrdinalIgnoreCase);
}
