using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Positions;
using TradingSystem.Domain.Enums;

namespace StrategyService.Execution;

public sealed class Bot8015TradeExecutor(
    IOptions<Bot8015Options> options,
    Bot8015OrderExecutionService botOrders,
    OrderExecutionService commonOrders,
    IPositionStore positionStore,
    ILogger<Bot8015TradeExecutor> logger)
    : IBotTradeExecutor
{
    private readonly Bot8015Options _options = options.Value;

    public string BotName => _options.BotName;

    public async Task<TradeExecutionResult> OpenAsync(
        string symbol,
        PositionSide side,
        string? source,
        CancellationToken cancellationToken)
    {
        EnsureSupportedSymbol(symbol);

        try
        {
            var position = await botOrders.OpenAsync(
                side,
                source,
                cancellationToken);

            await positionStore.SaveAsync(
                position,
                cancellationToken);

            return TradeExecutionResult.Success(
                position.ShortId,
                $"BOT8015 opened {side} position.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "BOT8015 failed to open position. Side={Side}, Symbol={Symbol}",
                side,
                symbol);

            return TradeExecutionResult.Failure(
                $"BOT8015 failed to open {side} position.",
                ex);
        }
    }

    public async Task<TradeExecutionResult> CloseAsync(
        string shortId,
        string reason,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        if (position is null)
        {
            return TradeExecutionResult.Failure(
                $"BOT8015 position '{shortId}' was not found.");
        }

        if (position.Closed)
        {
            return TradeExecutionResult.Success(
                shortId,
                "Position is already closed.");
        }

        try
        {
            await commonOrders.ClosePositionAsync(
                _options.BotName,
                position,
                cancellationToken);

            position.MarkClosed(reason);

            await positionStore.SaveAsync(
                position,
                cancellationToken);

            return TradeExecutionResult.Success(
                shortId,
                reason);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "BOT8015 failed to close position. ShortId={ShortId}, Reason={Reason}",
                shortId,
                reason);

            return TradeExecutionResult.Failure(
                $"BOT8015 failed to close position '{shortId}'.",
                ex);
        }
    }

    private void EnsureSupportedSymbol(string symbol)
    {
        if (!symbol.Equals(_options.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"BOT8015 supports only '{_options.Symbol}', but received '{symbol}'.");
        }
    }
}
