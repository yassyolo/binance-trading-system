using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8016.Configuration;
using TradingSystem.Application.Locking;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.Domain.Enums;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016PositionLifecycleService(
    IOptions<Bot8016Options> options,
    IPositionStore positionStore,
    Bot8016OrderExecutionService execution,
    IBinanceFuturesOrderClient orders,
    TradingSystem.Binance.Execution.BinanceProtectedPositionService commonOrders,
    IPositionLockProvider locks,
    IClock clock,
    IBotRuntimeConfigurationProvider runtimeConfigurations,
    TelegramNotificationService telegram,
    ILogger<Bot8016PositionLifecycleService> logger)
{
    private readonly Bot8016Options _options = options.Value;

    public async Task HandleTpFilledAsync(string shortId, decimal executedQuantity, CancellationToken cancellationToken)
    {
        if (await IsPaperAsync(cancellationToken))
        {
            logger.LogDebug("BOT8016 ignored Binance TP fill handling because the bot is in Paper environment. ShortId = {ShortId}", shortId);
            return;
        }

        var position = await positionStore.GetAsync(_options.BotName, shortId, cancellationToken);
        if (position is null || position.Closed || position.TpExecuted)
            return;

        position.MarkTpFilled(executedQuantity, clock.UtcNow);
        position.TpStatus = "FILLED";
        position.TpFilledAtUtc = clock.UtcNow;
        position.Stop3Pending = position.RemainingQuantity > 0;
        position.UpdatedAtUtc = clock.UtcNow;

        if (position.RemainingQuantity <= 0)
        {
            if (!string.IsNullOrWhiteSpace(position.SlOrderId))
                await orders.CancelAlgoOrderAsync(position.Symbol, position.SlOrderId, cancellationToken);

            position.ProtectiveActive = false;
            position.MarkClosed("TP_FULL_EXIT", clock.UtcNow);
        }

        await positionStore.SaveAsync(position, cancellationToken);
    }

    public async Task TryCreateStop3AfterBreakoutAsync(decimal oneMinuteClose, CancellationToken cancellationToken)
    {
        if (await IsPaperAsync(cancellationToken))
        {
            logger.LogDebug("BOT8016 skipped Binance STOP3 lifecycle because the bot is in Paper environment.");
            return;
        }

        var positions = await positionStore.GetAllAsync(_options.BotName, cancellationToken);
        foreach (var position in positions)
        {
            if (position.Closed || !position.TpExecuted || position.HighReached ||
                position.RemainingQuantity <= 0 || !position.Stop3Pending)
            {
                continue;
            }

            var breakout = position.Side switch
            {
                PositionSide.Long => position.SignalCandleHigh is > 0 && oneMinuteClose > position.SignalCandleHigh.Value,
                PositionSide.Short => position.SignalCandleLow is > 0 && oneMinuteClose < position.SignalCandleLow.Value,
                _ => false
            };

            if (!breakout)
                continue;

            await using var positionLock = await locks.TryAcquireAsync(
                _options.BotName,
                position.ShortId,
                TimeSpan.FromSeconds(20),
                cancellationToken);

            if (positionLock is null)
                continue;

            var latest = await positionStore.GetAsync(_options.BotName, position.ShortId, cancellationToken);
            if (latest is null || latest.Closed || latest.HighReached)
                continue;

            await execution.CreateStop3AfterBreakoutAsync(latest, cancellationToken);

            if (!string.IsNullOrWhiteSpace(latest.SlOrderId))
            {
                await orders.CancelAlgoOrderAsync(latest.Symbol, latest.SlOrderId, cancellationToken);
                latest.SlStatus = "CANCELED";
            }

            latest.UpdatedAtUtc = clock.UtcNow;
            await positionStore.SaveAsync(latest, cancellationToken);

            try
            {
                await telegram.SendAsync(
                    $"🛡️ {_options.BotName} STOP3 created after breakout" +
                    $"ID: {latest.ShortId} Side: { latest.Side} Close: { oneMinuteClose} STOP3: { latest.Stop3Current}",
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "BOT8016 STOP3 was created, but Telegram notification failed. ShortId = {ShortId}",
                    latest.ShortId);
            }
        }
    }

    public async Task ExitOnTeethCrossAsync(decimal oneMinuteClose, decimal teeth, CancellationToken cancellationToken)
    {
        if (await IsPaperAsync(cancellationToken))
        {
            logger.LogDebug("BOT8016 skipped Binance Teeth-cross exit because the bot is in Paper environment.");
            return;
        }

        var positions = await positionStore.GetAllAsync(_options.BotName, cancellationToken);
        foreach (var position in positions)
        {
            if (position.Closed || !position.HighReached || !position.Stop3Created || position.RemainingQuantity <= 0)
                continue;

            var shouldExit = position.Side switch
            {
                PositionSide.Long => oneMinuteClose < teeth,
                PositionSide.Short => oneMinuteClose > teeth,
                _ => false
            };

            if (!shouldExit)
                continue;

            await commonOrders.CloseAsync(position, cancellationToken);
            position.MarkClosed(
                position.Side == PositionSide.Long ? "EXIT_1M_BELOW_TEETH" : "EXIT_1M_ABOVE_TEETH",
                clock.UtcNow);
            await positionStore.SaveAsync(position, cancellationToken);

            logger.LogInformation(
                "BOT8016 exited on Teeth cross. ShortId = {ShortId}, Side = {Side}, Close = {Close}, Teeth = {Teeth}",
                position.ShortId, position.Side, oneMinuteClose, teeth);
        }
    }

    private async Task<bool> IsPaperAsync(CancellationToken cancellationToken)
    {
        var config = await runtimeConfigurations.GetAsync(_options.BotName, cancellationToken);
        if (config is null)
        {
            logger.LogWarning("BOT8016 lifecycle blocked because runtime config was not found.");
            return true;
        }

        return config.Environment.Equals("Paper", StringComparison.OrdinalIgnoreCase);
    }
}
