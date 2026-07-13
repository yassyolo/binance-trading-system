using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Resilience;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Services;

public sealed class Bot8011ManualPositionRecoveryService(
        IOptions<Bot8011Options> options,
        IPositionStore positionStore,
        IBinanceFuturesOrderClient orders,
        BinanceExchangeInfoService exchangeInfo,
        BinanceRetryService retry,
        ILogger<Bot8011ManualPositionRecoveryService> logger)
{
    private readonly Bot8011Options options = options.Value;

    public async Task<int> RecoverAsync(CancellationToken cancellationToken)
    {
        var existing = await positionStore.GetAllAsync(
            options.BotName,
            cancellationToken);

        var risks = await orders.GetPositionRiskAsync(
            options.Symbol,
            cancellationToken);

        var recovered = 0;

        foreach (var risk in risks.Where(x => x.IsOpen))
        {
            var side = risk.PositionSide.Equals("LONG", StringComparison.OrdinalIgnoreCase)
                ? PositionSide.Long
                : PositionSide.Short;

            var alreadyTracked = existing.Any(x =>
                !x.Closed &&
                x.Symbol.Equals(risk.Symbol, StringComparison.OrdinalIgnoreCase) &&
                x.Side == side);

            if (alreadyTracked)
                continue;

            var quantity = Math.Abs(risk.PositionAmount);

            quantity = await exchangeInfo.RoundQuantityAsync(
                risk.Symbol,
                quantity,
                cancellationToken);

            if (quantity <= 0)
                continue;

            var shortId = CreateShortId();
            var botPrefix = ShortBot(options.BotName);

            var tpClientId = $"{botPrefix}_TP_{shortId}";
            var slClientId = $"{botPrefix}_SL_{shortId}";

            var tpPrice = side == PositionSide.Long
                ? risk.EntryPrice * (1 + options.TakeProfitPercent / 100m)
                : risk.EntryPrice * (1 - options.TakeProfitPercent / 100m);

            var slPrice = side == PositionSide.Long
    ? risk.EntryPrice - options.InitialStopLossDistance
    : risk.EntryPrice + options.InitialStopLossDistance;

            tpPrice = await exchangeInfo.RoundPriceAsync(risk.Symbol, tpPrice, cancellationToken);
            slPrice = await exchangeInfo.RoundPriceAsync(risk.Symbol, slPrice, cancellationToken);

            var tpQuantity = await exchangeInfo.RoundQuantityAsync(
                risk.Symbol,
                quantity / 2m,
                cancellationToken);

            if (tpQuantity <= 0)
                continue;

            var tp = await retry.ExecuteAsync(
                "RECOVER_TP",
                ct => orders.PlaceLimitOrderAsync(
                    risk.Symbol,
                    ToCloseSide(side),
                    ToPositionSide(side),
                    tpQuantity,
                    tpPrice,
                    tpClientId,
                    ct),
                cancellationToken);

            var sl = await retry.ExecuteAsync(
                "RECOVER_SL",
                ct => orders.PlaceStopMarketAlgoOrderAsync(
                    risk.Symbol,
                    ToCloseSide(side),
                    ToPositionSide(side),
                    quantity,
                    slPrice,
                    slClientId,
                    ct),
                cancellationToken);

            var position = new BotPosition
            {
                BotName = options.BotName,
                ShortId = shortId,
                Symbol = risk.Symbol,
                Side = side,
                Mode = PositionMode.Hedge,

                Quantity = quantity,
                RemainingQuantity = quantity,
                EntryPrice = risk.EntryPrice,

                ParentClientId = null,
                ParentOrderId = null,
                ParentFilledAtUtc = DateTime.UtcNow,

                TpClientId = tpClientId,
                TpOrderId = tp.OrderId,
                TpPrice = tpPrice,
                TpStatus = tp.Status,

                SlClientId = slClientId,
                SlOrderId = sl.AlgoOrderId,
                SlPrice = slPrice,
                SlStatus = sl.Status,

                ProtectiveActive = true,
                ManualPosition = true,
                Source = "MANUAL_RECOVERY",
                Status = PositionStatus.Open,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await positionStore.SaveAsync(position, cancellationToken);

            recovered++;

            logger.LogWarning(
                "BOT8011 manual Binance position recovered. Position={ShortId}, Side={Side}, Qty={Qty}, Entry={Entry}",
                shortId,
                side,
                quantity,
                risk.EntryPrice);
        }

        return recovered;
    }

    private static string CreateShortId()
        => Guid.NewGuid().ToString("N")[..10];

    private static string ShortBot(string botName)
        => botName.Length > 8 ? botName[..8] : botName;

    private static string ToCloseSide(PositionSide side)
        => side == PositionSide.Long ? "SELL" : "BUY";

    private static string ToPositionSide(PositionSide side)
        => side == PositionSide.Long ? "LONG" : "SHORT";
}