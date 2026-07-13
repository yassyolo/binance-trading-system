using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Positions;
using TradingSystem.Domain.Enums;

namespace StrategyService.Execution;

public sealed class Bot8012TradeExecutor(
    IOptions<Bot8012Options> options,
    OrderExecutionService orders,
    IPositionStore positionStore,
    ILogger<Bot8012TradeExecutor> logger)
    : IBotTradeExecutor
{
    private readonly Bot8012Options _options = options.Value;

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
            var position = await orders.OpenTpOnlyPositionAsync(
                _options.BotName,
                side,
                symbol,
                _options.Quantity,
                _options.ProfitDistance,
                cancellationToken);

            position.Source = source;

            await positionStore.SaveAsync(
                position,
                cancellationToken);

            logger.LogInformation(
                "BOT8012 position opened. Position={ShortId}, Side={Side}, Symbol={Symbol}, Source={Source}",
                position.ShortId,
                position.Side,
                position.Symbol,
                source);

            return TradeExecutionResult.Success(
                position.ShortId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "BOT8012 failed to open position. Side={Side}, Symbol={Symbol}",
                side,
                symbol);

            return TradeExecutionResult.Failure(
                $"BOT8012 failed to open {side} position.",
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
                $"Position '{shortId}' was not found.");
        }

        if (position.Closed)
        {
            return TradeExecutionResult.Success(
                shortId,
                "Position is already closed.");
        }

        try
        {
            await orders.ClosePositionAsync(
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
                "BOT8012 failed to close position. Position={ShortId}, Reason={Reason}",
                shortId,
                reason);

            return TradeExecutionResult.Failure(
                $"BOT8012 failed to close position '{shortId}'.",
                ex);
        }
    }

    private void EnsureSupportedSymbol(string symbol)
    {
        if (!symbol.Equals(
                _options.Symbol,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"BOT8012 supports only '{_options.Symbol}', but received '{symbol}'.");
        }
    }
}
