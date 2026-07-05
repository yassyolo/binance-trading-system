using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Services;

public sealed class Bot8011PositionEventService
{
    private readonly Bot8011Options _options;
    private readonly IPositionStore _positionStore;
    private readonly IBinanceFuturesOrderClient _binanceOrders;
    private readonly Bot8011Stop3OrderService _stop3Orders;
    private readonly ILogger<Bot8011PositionEventService> _logger;

    public Bot8011PositionEventService(
        IOptions<Bot8011Options> options,
        IPositionStore positionStore,
        IBinanceFuturesOrderClient binanceOrders,
        Bot8011Stop3OrderService stop3Orders,
        ILogger<Bot8011PositionEventService> logger)
    {
        _options = options.Value;
        _positionStore = positionStore;
        _binanceOrders = binanceOrders;
        _stop3Orders = stop3Orders;
        _logger = logger;
    }

    public async Task HandleTpFilledAsync(
        string shortId,
        decimal executedQuantity,
        CancellationToken cancellationToken)
    {
        var position = await LoadPositionAsync(shortId, cancellationToken);

        if (position is null)
            return;

        if (position.TpExecuted || position.Stop3Created || position.Stop3Pending)
            return;

        position.MarkTpFilled(executedQuantity);

        if (position.RemainingQuantity <= 0)
        {
            await CancelInitialSlIfExistsAsync(position, cancellationToken);
            await MarkClosedAsync(position, "TP_FULL_EXIT", cancellationToken);
            return;
        }

        position.Stop3Pending = true;
        position.ProtectiveActive = true;
        position.Status = PositionStatus.Stop3Pending;
        position.UpdatedAtUtc = DateTime.UtcNow;

        await _positionStore.SaveAsync(position, cancellationToken);

        try
        {
            await CancelInitialSlIfExistsAsync(position, cancellationToken);

            var stop3ClientId = CreateClientId(_options.BotName, "S3", position.ShortId);

            var stop3 = await _stop3Orders.CreateStop3WithFallbackAsync(
                position,
                position.RemainingQuantity,
                stop3ClientId,
                cancellationToken);

            position.Stop3ClientId = stop3ClientId;
            position.Stop3OrderId = stop3.AlgoOrderId;
            position.Stop3Initial = stop3.TriggerPrice;
            position.Stop3Current = stop3.TriggerPrice;
            position.Stop3Previous = stop3.TriggerPrice;
            position.Stop3Status = stop3.Status;
            position.Stop3Created = true;
            position.Stop3Pending = false;
            position.ProtectiveActive = true;
            position.Status = PositionStatus.Stop3Active;
            position.UpdatedAtUtc = DateTime.UtcNow;

            await _positionStore.SaveAsync(position, cancellationToken);

            _logger.LogInformation(
                "BOT8011 TP filled and STOP3 created. Position={ShortId}, Remaining={Remaining}, Stop3={Stop3}",
                position.ShortId,
                position.RemainingQuantity,
                stop3.TriggerPrice);
        }
        catch (Exception ex)
        {
            position.Stop3Pending = true;
            position.Stop3Created = false;
            position.Status = PositionStatus.Stop3Pending;
            position.UpdatedAtUtc = DateTime.UtcNow;

            await _positionStore.SaveAsync(position, cancellationToken);

            _logger.LogError(
                ex,
                "BOT8011 failed to create STOP3 after TP. Position={ShortId}",
                position.ShortId);

            throw;
        }
    }

    public async Task HandleSlTriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
    {
        var position = await LoadPositionAsync(shortId, cancellationToken);

        if (position is null)
            return;

        position.SlExecuted = true;
        position.SlStatus = "FILLED";
        position.SlTriggeredAtUtc = DateTime.UtcNow;
        position.Stop3Pending = false;
        position.TrailingInProgress = false;
        position.ProtectiveActive = false;

        await MarkClosedAsync(position, "SL_TRIGGERED", cancellationToken);
    }

    public async Task HandleStop3TriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
    {
        var position = await LoadPositionAsync(shortId, cancellationToken);

        if (position is null)
            return;

        position.Stop3Status = "FILLED";
        position.Stop3TriggeredAtUtc = DateTime.UtcNow;
        position.Stop3Pending = false;
        position.TrailingInProgress = false;
        position.ProtectiveActive = false;

        await MarkClosedAsync(position, "STOP3_TRIGGERED", cancellationToken);
    }

    private async Task<BotPosition?> LoadPositionAsync(
        string shortId,
        CancellationToken cancellationToken)
    {
        var position = await _positionStore.GetAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        if (position is null || position.Closed)
            return null;

        return position;
    }

    private async Task CancelInitialSlIfExistsAsync(
        BotPosition position,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(position.SlOrderId) || position.SlExecuted)
            return;

        await _binanceOrders.CancelAlgoOrderAsync(
            position.Symbol,
            position.SlOrderId,
            cancellationToken);

        position.SlStatus = "CANCELED";
        position.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task MarkClosedAsync(
        BotPosition position,
        string reason,
        CancellationToken cancellationToken)
    {
        position.MarkClosed(reason);
        position.ProtectiveActive = false;
        position.Stop3Pending = false;
        position.TrailingInProgress = false;

        await _positionStore.SaveAsync(position, cancellationToken);

        _logger.LogInformation(
            "BOT8011 position closed. Position={ShortId}, Reason={Reason}",
            position.ShortId,
            reason);
    }

    private static string CreateClientId(string botName, string prefix, string shortId)
    {
        var shortBot = botName.Length > 8 ? botName[..8] : botName;
        var value = $"{shortBot}_{prefix}_{shortId}";

        return value[..Math.Min(32, value.Length)];
    }
}