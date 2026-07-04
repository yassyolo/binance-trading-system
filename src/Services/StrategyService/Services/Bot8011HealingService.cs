using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Enums;

namespace StrategyService.Services;

public sealed class Bot8011HealingService
{
    private readonly Bot8011Options _options;
    private readonly IPositionStore _positionStore;
    private readonly IBinanceFuturesOrderClient _orders;
    private readonly SafeBinanceOrderService _safeOrders;
    private readonly ILogger<Bot8011HealingService> _logger;

    public Bot8011HealingService(
        IOptions<Bot8011Options> options,
        IPositionStore positionStore,
        IBinanceFuturesOrderClient orders,
        SafeBinanceOrderService safeOrders,
        ILogger<Bot8011HealingService> logger)
    {
        _options = options.Value;
        _positionStore = positionStore;
        _orders = orders;
        _safeOrders = safeOrders;
        _logger = logger;
    }

    public async Task HealAsync(
        HealingSnapshotDto snapshot,
        CancellationToken cancellationToken)
    {
        if (!snapshot.Type.Equals("healing_snapshot", StringComparison.OrdinalIgnoreCase))
            return;

        if (!snapshot.Symbol.Equals(_options.Symbol, StringComparison.OrdinalIgnoreCase))
            return;

        var activeIds = snapshot.ActiveClientIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var positions = await _positionStore.GetAllAsync(
            _options.BotName,
            cancellationToken);

        foreach (var position in positions.Where(x => !x.Closed))
        {
            var tpPresent = IsPresent(activeIds, position.TpClientId);
            var slPresent = IsPresent(activeIds, position.SlClientId);
            var stop3Present = IsPresent(activeIds, position.Stop3ClientId);

            if (tpPresent && slPresent && !position.TpExecuted)
            {
                _logger.LogInformation(
                    "BOT8011 healing OK. Position={ShortId}, Mode=TP_SL",
                    position.ShortId);

                continue;
            }

            if (!tpPresent && slPresent && !position.TpExecuted)
            {
                await HealTpHitAsync(position, cancellationToken);
                continue;
            }

            if (tpPresent && !slPresent && !position.TpExecuted)
            {
                await HealSlHitAsync(position, cancellationToken);
                continue;
            }

            if (position.TpExecuted && position.ProtectiveActive)
            {
                if (stop3Present)
                {
                    _logger.LogInformation(
                        "BOT8011 healing OK. Position={ShortId}, Mode=STOP3",
                        position.ShortId);

                    continue;
                }

                await HealStop3HitAsync(position, cancellationToken);
                continue;
            }

            if (!tpPresent && !slPresent && !stop3Present)
            {
                await _positionStore.DeleteAsync(
                    _options.BotName,
                    position.ShortId,
                    cancellationToken);

                _logger.LogWarning(
                    "BOT8011 healing deleted ghost position. Position={ShortId}",
                    position.ShortId);
            }
        }
    }

    private async Task HealTpHitAsync(
        TradingSystem.Domain.Positions.BotPosition position,
        CancellationToken cancellationToken)
    {
        var executedQuantity = position.Quantity / 2m;
        var remaining = Math.Max(position.Quantity - executedQuantity, 0);

        position.TpExecuted = true;
        position.TpStatus = "FILLED";
        position.TpFilledAtUtc = DateTime.UtcNow;
        position.RemainingQuantity = remaining;
        position.ProtectiveActive = true;
        position.Stop3Pending = true;
        position.Status = PositionStatus.Stop3Pending;
        position.UpdatedAtUtc = DateTime.UtcNow;

        await _positionStore.SaveAsync(position, cancellationToken);

        if (remaining <= 0)
        {
            position.MarkClosed("HEALING_TP_FULL_EXIT");
            await _positionStore.SaveAsync(position, cancellationToken);
            return;
        }

        if (!position.EntryPrice.HasValue)
        {
            _logger.LogWarning(
                "BOT8011 healing TP hit but missing entry price. Position={ShortId}",
                position.ShortId);

            return;
        }

        var stop3ClientId = CreateClientId(_options.BotName, "S3", position.ShortId);

        var stop3Price = position.Side == PositionSide.Long
            ? position.EntryPrice.Value + _options.Stop3EntryOffset
            : position.EntryPrice.Value - _options.Stop3EntryOffset;

        var stop3 = await _orders.PlaceStopMarketAlgoOrderAsync(
            position.Symbol,
            ToCloseSide(position.Side),
            ToPositionSide(position.Side),
            remaining,
            stop3Price,
            stop3ClientId,
            cancellationToken);

        position.Stop3ClientId = stop3ClientId;
        position.Stop3OrderId = stop3.AlgoOrderId;
        position.Stop3Initial = stop3Price;
        position.Stop3Current = stop3Price;
        position.Stop3Previous = stop3Price;
        position.Stop3Status = stop3.Status;
        position.Stop3Created = true;
        position.Stop3Pending = false;
        position.Status = PositionStatus.Stop3Active;
        position.UpdatedAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(position.SlOrderId))
        {
            await _safeOrders.SafeCancelAlgoAsync(
                position.Symbol,
                position.SlOrderId,
                position.SlClientId,
                cancellationToken);

            position.SlStatus = "CANCELED";
        }

        await _positionStore.SaveAsync(position, cancellationToken);

        _logger.LogWarning(
            "BOT8011 healing detected TP hit and created STOP3. Position={ShortId}, Stop3={Stop3}",
            position.ShortId,
            stop3Price);
    }

    private async Task HealSlHitAsync(
        TradingSystem.Domain.Positions.BotPosition position,
        CancellationToken cancellationToken)
    {
        position.SlExecuted = true;
        position.SlStatus = "FILLED";
        position.SlTriggeredAtUtc = DateTime.UtcNow;
        position.ProtectiveActive = false;
        position.Stop3Pending = false;
        position.TrailingInProgress = false;
        position.MarkClosed("HEALING_SL_HIT");

        await _positionStore.SaveAsync(position, cancellationToken);

        _logger.LogWarning(
            "BOT8011 healing detected SL hit. Position={ShortId}",
            position.ShortId);
    }

    private async Task HealStop3HitAsync(
        TradingSystem.Domain.Positions.BotPosition position,
        CancellationToken cancellationToken)
    {
        position.Stop3Status = "FILLED";
        position.Stop3TriggeredAtUtc = DateTime.UtcNow;
        position.ProtectiveActive = false;
        position.Stop3Pending = false;
        position.TrailingInProgress = false;
        position.MarkClosed("HEALING_STOP3_HIT");

        await _positionStore.SaveAsync(position, cancellationToken);

        _logger.LogWarning(
            "BOT8011 healing detected STOP3 hit. Position={ShortId}",
            position.ShortId);
    }

    private static bool IsPresent(HashSet<string> activeIds, string? clientId)
        => !string.IsNullOrWhiteSpace(clientId) &&
           activeIds.Contains(clientId);

    private static string CreateClientId(string botName, string prefix, string shortId)
    {
        var shortBot = botName.Length > 8 ? botName[..8] : botName;
        var value = $"{shortBot}_{prefix}_{shortId}";

        return value[..Math.Min(32, value.Length)];
    }

    private static string ToCloseSide(PositionSide side)
        => side == PositionSide.Long ? "SELL" : "BUY";

    private static string ToPositionSide(PositionSide side)
        => side == PositionSide.Long ? "LONG" : "SHORT";
}