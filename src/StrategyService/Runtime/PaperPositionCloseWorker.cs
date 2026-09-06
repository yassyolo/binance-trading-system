using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.PaperTrading.Contracts;
using TradingSystem.PaperTrading.Executor;
using TradingSystem.PaperTrading.Models;

namespace StrategyService.Runtime;

public sealed class PaperPositionCloseWorker(
    IPaperTradingStore store,
    IMarketPriceProvider marketPriceProvider,
    PaperTradeExecutor paperTradeExecutor,
    ILogger<PaperPositionCloseWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(InitialDelay, ct);

            using var timer = new PeriodicTimer(CheckInterval);

            logger.LogInformation("{Worker} started. CheckInterval = {CheckInterval}", nameof(PaperPositionCloseWorker), CheckInterval);

            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    await ProcessOpenPositionsAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error while processing open paper positions.");
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        { }
        finally
        {
            logger.LogInformation("{Worker} stopped.", nameof(PaperPositionCloseWorker));
        }
    }

    private async Task ProcessOpenPositionsAsync(CancellationToken ct)
    {
        var openPositions = await store.GetOpenAsync(ct);
        if (openPositions.Count == 0)
            return;

        foreach (var symbolGroup in openPositions.GroupBy(p => p.Symbol, StringComparer.OrdinalIgnoreCase))
        {
            ct.ThrowIfCancellationRequested();

            await ProcessSymbolPositionsAsync(symbolGroup.Key, symbolGroup, ct);
        }
    }

    private async Task ProcessSymbolPositionsAsync(string symbol, IEnumerable<PaperTradingPosition> positions, CancellationToken ct)
    {
        decimal markPrice;
        try
        {
            markPrice = await marketPriceProvider.GetMarkPriceAsync(symbol, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to obtain mark price while evaluating paper positions. Symbol = {Symbol}", symbol);
            return;
        }

        if (markPrice <= 0)
        {
            logger.LogWarning("Paper p evaluation skipped because mark price is invalid. Symbol = {Symbol}, MarkPrice = {MarkPrice}", symbol, markPrice);
            return;
        }

        foreach (var position in positions)
        {
            ct.ThrowIfCancellationRequested();

            await ProcessPositionAsync(position, markPrice, ct);
        }
    }

    private async Task ProcessPositionAsync(PaperTradingPosition p, decimal markPrice, CancellationToken ct)
    {
        if (!HasValidExitLevels(p))
        {
            logger.LogError("Paper p has invalid exit levels. Bot = {Bot}, Position = {Position}, Side = {Side}, Entry = {Entry}, TakeProfit = {TakeProfit}, StopLoss = {StopLoss}",
                p.BotName,
                p.ShortId,
                p.Side,
                p.EntryPrice,
                p.TakeProfitPrice,
                p.StopLossPrice);
            return;
        }

        var closeReason = ResolveCloseReason(p, markPrice);
        if (closeReason is null)
            return;

        logger.LogInformation("Paper exit condition reached. Bot = {Bot}, Position = {Position}, Symbol = {Symbol}, " +
            "Side = {Side}, MarkPrice = {MarkPrice}, TakeProfit = {TakeProfit}, StopLoss = {StopLoss}, Reason = {Reason}",
            p.BotName,
            p.ShortId,
            p.Symbol,
            p.Side,
            markPrice,
            p.TakeProfitPrice,
            p.StopLossPrice,
            closeReason);

        var result = await paperTradeExecutor.CloseAtPriceAsync(p.BotName, p.ShortId, markPrice, closeReason, ct);
        if (result.Succeeded)
        {
            logger.LogInformation("Paper position closed successfully. Bot = {Bot}, Position = {Position}, TriggerPrice = {TriggerPrice}, Reason = {Reason}", p.BotName, p.ShortId, markPrice, closeReason);
            return;
        }

        logger.LogWarning("Paper position close failed. Bot = {Bot}, Position = {Position}, TriggerPrice = {TriggerPrice}, Reason = {Reason}, Result = {Result}", p.BotName, p.ShortId, markPrice, closeReason, result.Reason);
    }

    private static bool HasValidExitLevels(PaperTradingPosition position)
    {
        if (position.EntryPrice <= 0 ||  position.Quantity <= 0)
            return false;

        return position.Side switch
        {
            PositionSide.Long => IsValidLongTakeProfit(position) && IsValidLongStopLoss(position),
            PositionSide.Short => IsValidShortTakeProfit(position) && IsValidShortStopLoss(position),
            _ => false
        };
    }

    private static bool IsValidLongTakeProfit(PaperTradingPosition position)
        => position.TakeProfitPrice == default || position.TakeProfitPrice > position.EntryPrice;

    private static bool IsValidLongStopLoss(PaperTradingPosition position)
        => position.StopLossPrice == default || position.StopLossPrice < position.EntryPrice;

    private static bool IsValidShortTakeProfit(PaperTradingPosition position)
        => position.TakeProfitPrice == default || position.TakeProfitPrice < position.EntryPrice;

    private static bool IsValidShortStopLoss(PaperTradingPosition position)
        => position.StopLossPrice == default || position.StopLossPrice > position.EntryPrice;

    private static string? ResolveCloseReason(PaperTradingPosition position, decimal markPrice)
    {
        return position.Side switch
        {
            PositionSide.Long
                when position.TakeProfitPrice != default && markPrice >= position.TakeProfitPrice
                => "PAPER_TAKE_PROFIT",
            PositionSide.Long
                when position.StopLossPrice != default && markPrice <= position.StopLossPrice
                => "PAPER_STOP_LOSS",
            PositionSide.Short
                when position.TakeProfitPrice != default && markPrice <= position.TakeProfitPrice
                => "PAPER_TAKE_PROFIT",
            PositionSide.Short
                when position.StopLossPrice != default && markPrice >= position.StopLossPrice
                => "PAPER_STOP_LOSS",
            _ => null
        };
    }
}