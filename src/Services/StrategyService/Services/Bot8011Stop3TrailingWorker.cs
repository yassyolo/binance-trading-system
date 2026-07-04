using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Market;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Enums;

namespace StrategyService.Services;

public sealed class Bot8011Stop3TrailingWorker : BackgroundService
{
    private readonly Bot8011Options _options;
    private readonly IPositionStore _positionStore;
    private readonly IBinanceFuturesMarketClient _marketClient;
    private readonly IBinanceFuturesOrderClient _orderClient;
    private readonly ILogger<Bot8011Stop3TrailingWorker> _logger;

    public Bot8011Stop3TrailingWorker(
        IOptions<Bot8011Options> options,
        IPositionStore positionStore,
        IBinanceFuturesMarketClient marketClient,
        IBinanceFuturesOrderClient orderClient,
        ILogger<Bot8011Stop3TrailingWorker> logger)
    {
        _options = options.Value;
        _positionStore = positionStore;
        _marketClient = marketClient;
        _orderClient = orderClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BOT8011 STOP3 trailing worker failed.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.Stop3TrailingIntervalSeconds),
                stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var positions = await _positionStore.GetAllAsync(
            _options.BotName,
            cancellationToken);

        var stop3Positions = positions
            .Where(x =>
                !x.Closed &&
                x.Stop3Created &&
                !x.Stop3Pending &&
                !x.TrailingInProgress &&
                x.Stop3NewPending == null &&
                !string.IsNullOrWhiteSpace(x.Stop3OrderId) &&
                x.Stop3Current.HasValue)
            .ToList();

        foreach (var position in stop3Positions)
        {
            var markPrice = await _marketClient.GetMarkPriceAsync(
                position.Symbol,
                cancellationToken);

            var newStop3Price = CalculateNewStop3Price(
                position.Side,
                markPrice,
                position.Stop3Current.Value);

            if (newStop3Price == position.Stop3Current.Value)
                continue;

            var oldStop3Price = position.Stop3Current.Value;
            var oldStop3OrderId = position.Stop3OrderId!;

            position.TrailingInProgress = true;
            position.Stop3NewPending = newStop3Price;
            position.UpdatedAtUtc = DateTime.UtcNow;

            await _positionStore.SaveAsync(position, cancellationToken);

            try
            {
                await _orderClient.CancelAlgoOrderAsync(
                    position.Symbol,
                    oldStop3OrderId,
                    cancellationToken);

                var clientOrderId = CreateClientId(
                    _options.BotName,
                    "S3",
                    position.ShortId);

                var newStop3 = await _orderClient.PlaceStopMarketAlgoOrderAsync(
                    position.Symbol,
                    ToCloseSide(position.Side),
                    ToPositionSide(position.Side),
                    position.RemainingQuantity,
                    newStop3Price,
                    clientOrderId,
                    cancellationToken);

                position.Stop3Previous = oldStop3Price;
                position.Stop3Current = newStop3Price;
                position.Stop3OrderId = newStop3.AlgoOrderId;
                position.Stop3ClientId = clientOrderId;
                position.Stop3Status = newStop3.Status;
                position.Stop3NewPending = null;
                position.TrailingInProgress = false;
                position.TrailCount++;
                position.UpdatedAtUtc = DateTime.UtcNow;

                await _positionStore.SaveAsync(position, cancellationToken);

                _logger.LogInformation(
                    "BOT8011 STOP3 moved. Position={ShortId}, Mark={MarkPrice}, Old={OldStop}, New={NewStop}",
                    position.ShortId,
                    markPrice,
                    oldStop3Price,
                    newStop3Price);
            }
            catch (Exception ex)
            {
                position.TrailingInProgress = false;
                position.Stop3NewPending = null;
                position.UpdatedAtUtc = DateTime.UtcNow;

                await _positionStore.SaveAsync(position, cancellationToken);

                _logger.LogError(
                    ex,
                    "BOT8011 STOP3 move failed. Position={ShortId}, Mark={MarkPrice}, Old={OldStop}, New={NewStop}",
                    position.ShortId,
                    markPrice,
                    oldStop3Price,
                    newStop3Price);

                throw;
            }
        }
    }

    private decimal CalculateNewStop3Price(
        PositionSide side,
        decimal markPrice,
        decimal currentStop3)
    {
        var distance = _options.Stop3TrailingDistance;

        var candidate = side == PositionSide.Long
            ? markPrice - distance
            : markPrice + distance;

        if (side == PositionSide.Long)
            return candidate > currentStop3 ? candidate : currentStop3;

        return candidate < currentStop3 ? candidate : currentStop3;
    }

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