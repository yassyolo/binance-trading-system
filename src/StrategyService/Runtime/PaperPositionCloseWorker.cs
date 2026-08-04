using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingSystem.Application.Engine;
using TradingSystem.Domain.Enums;
using TradingSystem.PaperTrading;
using TradingSystem.PaperTrading.Executor;

namespace StrategyService.Runtime;

public sealed class PaperPositionCloseWorker(
    IPaperTradingStore store,
    IMarketPriceProvider prices,
    PaperTradeExecutor executor,
    ILogger<PaperPositionCloseWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan InitialDelay =
        TimeSpan.FromSeconds(5);

    private static readonly TimeSpan CheckInterval =
        TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(
                InitialDelay,
                stoppingToken);

            using var timer = new PeriodicTimer(
                CheckInterval);

            logger.LogInformation(
                "{Worker} started. CheckInterval = {CheckInterval}",
                nameof(PaperPositionCloseWorker),
                CheckInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessOpenPositionsAsync(
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Unexpected error while processing open paper positions.");
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Expected when the host is stopping during the initial delay.
        }
        finally
        {
            logger.LogInformation(
                "{Worker} stopped.",
                nameof(PaperPositionCloseWorker));
        }
    }

    private async Task ProcessOpenPositionsAsync(
        CancellationToken ct)
    {
        var openPositions = await store.GetOpenAsync(ct);

        if (openPositions.Count == 0)
            return;

        var positionsBySymbol = openPositions.GroupBy(
            position => position.Symbol,
            StringComparer.OrdinalIgnoreCase);

        foreach (var symbolGroup in positionsBySymbol)
        {
            ct.ThrowIfCancellationRequested();

            await ProcessSymbolPositionsAsync(
                symbolGroup.Key,
                symbolGroup,
                ct);
        }
    }

    private async Task ProcessSymbolPositionsAsync(
        string symbol,
        IEnumerable<PaperTradingPosition> positions,
        CancellationToken ct)
    {
        decimal markPrice;

        try
        {
            markPrice = await prices.GetMarkPriceAsync(
                symbol,
                ct);
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to obtain mark price while evaluating paper positions. Symbol = {Symbol}",
                symbol);

            return;
        }

        if (markPrice <= 0)
        {
            logger.LogWarning(
                "Paper position evaluation skipped because mark price is invalid. Symbol = {Symbol}, MarkPrice = {MarkPrice}",
                symbol,
                markPrice);

            return;
        }

        foreach (var position in positions)
        {
            ct.ThrowIfCancellationRequested();

            await ProcessPositionAsync(
                position,
                markPrice,
                ct);
        }
    }

    private async Task ProcessPositionAsync(
        PaperTradingPosition position,
        decimal markPrice,
        CancellationToken ct)
    {
        if (!HasValidExitLevels(position))
        {
            LogInvalidExitLevels(position);
            return;
        }

        var closeReason = ResolveCloseReason(
            position,
            markPrice);

        if (closeReason is null)
            return;

        logger.LogInformation(
            "Paper exit condition reached. " +
            "Bot = {Bot}, Position = {Position}, Symbol = {Symbol}, " +
            "Side = {Side}, MarkPrice = {MarkPrice}, " +
            "TakeProfit = {TakeProfit}, StopLoss = {StopLoss}, " +
            "Reason = {Reason}",
            position.BotName,
            position.ShortId,
            position.Symbol,
            position.Side,
            markPrice,
            position.TakeProfitPrice,
            position.StopLossPrice,
            closeReason);

        var result = await executor.CloseAtPriceAsync(
            position.BotName,
            position.ShortId,
            markPrice,
            closeReason,
            ct);

        if (result.Succeeded)
        {
            logger.LogInformation(
                "Paper position closed successfully. " +
                "Bot = {Bot}, Position = {Position}, " +
                "TriggerPrice = {TriggerPrice}, Reason = {Reason}",
                position.BotName,
                position.ShortId,
                markPrice,
                closeReason);

            return;
        }

        logger.LogWarning(
            "Paper position close failed. " +
            "Bot = {Bot}, Position = {Position}, " +
            "TriggerPrice = {TriggerPrice}, Reason = {Reason}, " +
            "Result = {Result}",
            position.BotName,
            position.ShortId,
            markPrice,
            closeReason,
            result.Reason);
    }

    private void LogInvalidExitLevels(
        PaperTradingPosition position)
    {
        logger.LogError(
            "Paper position has invalid exit levels. " +
            "Bot = {Bot}, Position = {Position}, Side = {Side}, " +
            "Entry = {Entry}, TakeProfit = {TakeProfit}, StopLoss = {StopLoss}",
            position.BotName,
            position.ShortId,
            position.Side,
            position.EntryPrice,
            position.TakeProfitPrice,
            position.StopLossPrice);
    }

    private static bool HasValidExitLevels(
        PaperTradingPosition position)
    {
        if (position.EntryPrice <= 0 ||
            position.Quantity <= 0)
        {
            return false;
        }

        return position.Side switch
        {
            PositionSide.Long =>
                IsValidLongTakeProfit(position) &&
                IsValidLongStopLoss(position),

            PositionSide.Short =>
                IsValidShortTakeProfit(position) &&
                IsValidShortStopLoss(position),

            _ => false
        };
    }

    private static bool IsValidLongTakeProfit(
        PaperTradingPosition position)
    {
        return position.TakeProfitPrice == default ||
               position.TakeProfitPrice > position.EntryPrice;
    }

    private static bool IsValidLongStopLoss(
        PaperTradingPosition position)
    {
        return position.StopLossPrice == default ||
               position.StopLossPrice < position.EntryPrice;
    }

    private static bool IsValidShortTakeProfit(
        PaperTradingPosition position)
    {
        return position.TakeProfitPrice == default ||
               position.TakeProfitPrice < position.EntryPrice;
    }

    private static bool IsValidShortStopLoss(
        PaperTradingPosition position)
    {
        return position.StopLossPrice == default ||
               position.StopLossPrice > position.EntryPrice;
    }

    private static string? ResolveCloseReason(
        PaperTradingPosition position,
        decimal markPrice)
    {
        return position.Side switch
        {
            PositionSide.Long
                when position.TakeProfitPrice != default &&
                     markPrice >= position.TakeProfitPrice
                => "PAPER_TAKE_PROFIT",

            PositionSide.Long
                when position.StopLossPrice != default &&
                     markPrice <= position.StopLossPrice
                => "PAPER_STOP_LOSS",

            PositionSide.Short
                when position.TakeProfitPrice != default &&
                     markPrice <= position.TakeProfitPrice
                => "PAPER_TAKE_PROFIT",

            PositionSide.Short
                when position.StopLossPrice != default &&
                     markPrice >= position.StopLossPrice
                => "PAPER_STOP_LOSS",

            _ => null
        };
    }
}