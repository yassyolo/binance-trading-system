using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Positions;
using TradingSystem.Domain.Enums;

namespace StrategyService.Execution;

public sealed class Bot8011TradeExecutor(
    IOptions<Bot8011Options> options,
    OrderExecutionService orders,
    IPositionStore positionStore,
    ILogger<Bot8011TradeExecutor> logger)
    : IBotTradeExecutor
{
    private readonly Bot8011Options _options = options.Value;

    public string BotName => _options.BotName;

    public async Task<TradeExecutionResult> OpenAsync(
        string symbol,
        PositionSide side,
        string? source,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.Equals(
                    symbol,
                    _options.Symbol,
                    StringComparison.OrdinalIgnoreCase))
            {
                return TradeExecutionResult.Failure(
                    $"BOT8011 does not support symbol '{symbol}'. Expected '{_options.Symbol}'.");
            }

            var position = await orders.OpenBot8011PositionAsync(
                side,
                _options,
                cancellationToken);

            position.Source = string.IsNullOrWhiteSpace(source)
                ? "webhook"
                : source;

            await positionStore.SaveAsync(
                position,
                cancellationToken);

            logger.LogInformation(
                "BOT8011 opened position. ShortId={ShortId}, Side={Side}, Source={Source}",
                position.ShortId,
                position.Side,
                position.Source);

            return TradeExecutionResult.Success(
                position.ShortId,
                $"BOT8011 {side} position opened successfully.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "BOT8011 failed to open position. Symbol={Symbol}, Side={Side}",
                symbol,
                side);

            return TradeExecutionResult.Failure(
                $"Failed to open BOT8011 position: {ex.Message}",
                ex);
        }
    }

    public async Task<TradeExecutionResult> CloseAsync(
        string shortId,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            var position = await positionStore.GetAsync(
                _options.BotName,
                shortId,
                cancellationToken);

            if (position is null)
            {
                logger.LogWarning(
                    "BOT8011 close skipped. Position not found. ShortId={ShortId}, Reason={Reason}",
                    shortId,
                    reason);

                return TradeExecutionResult.Failure(
                    $"BOT8011 position '{shortId}' was not found.");
            }

            if (position.Closed)
            {
                logger.LogInformation(
                    "BOT8011 close skipped. Position already closed. ShortId={ShortId}",
                    shortId);

                return TradeExecutionResult.Success(
                    shortId,
                    $"BOT8011 position '{shortId}' is already closed.");
            }

            await orders.ClosePositionAsync(
                _options.BotName,
                position,
                cancellationToken);

            position.MarkClosed(reason);

            await positionStore.SaveAsync(
                position,
                cancellationToken);

            logger.LogInformation(
                "BOT8011 closed position. ShortId={ShortId}, Reason={Reason}",
                shortId,
                reason);

            return TradeExecutionResult.Success(
                shortId,
                $"BOT8011 position '{shortId}' closed successfully. Reason: {reason}");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "BOT8011 failed to close position. ShortId={ShortId}, Reason={Reason}",
                shortId,
                reason);

            return TradeExecutionResult.Failure(
                $"Failed to close BOT8011 position '{shortId}': {ex.Message}",
                ex);
        }
    }
}