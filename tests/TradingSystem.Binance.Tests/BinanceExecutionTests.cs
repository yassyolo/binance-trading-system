using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Exchange;
using TradingSystem.Binance.Execution;
using TradingSystem.Binance.Orders;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Orders.Models;
using TradingSystem.Domain.Enums;
using Xunit;

namespace TradingSystem.Binance.Tests;

public sealed class BinanceExecutionTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SafePlaceMarketOrderAsync_WhenSubmitSucceeds_ReturnsSubmittedOrder()
    {
        var client = new FakeOrderClient
        {
            MarketOrder = Order("p1", "cid", "FILLED", 100m, 1m, 100m)
        };
        var sut = Safe(client);

        var result = await sut.SafePlaceMarketOrderAsync("BTCUSDC", "BUY", "LONG", 1m, "cid", default);

        Assert.Equal("p1", result.OrderId);
        Assert.Equal(1, client.PlaceMarketCalls);
        Assert.Equal(0, client.GetByClientIdCalls);
    }

    [Fact]
    public async Task SafePlaceMarketOrderAsync_WhenSubmitOutcomeUnknown_RecoversByClientOrderIdWithoutResubmit()
    {
        var recovered = Order("p1", "cid", "FILLED", 100m, 1m, 100m);
        var client = new FakeOrderClient
        {
            MarketOrderException = new HttpRequestException("connection lost"),
            OrderByClientId = recovered
        };
        var sut = Safe(client);

        var result = await sut.SafePlaceMarketOrderAsync("BTCUSDC", "BUY", "LONG", 1m, "cid", default);

        Assert.Same(recovered, result);
        Assert.Equal(1, client.PlaceMarketCalls);
        Assert.Equal(1, client.GetByClientIdCalls);
    }

    [Fact]
    public async Task WaitForFillAsync_FilledOrderWithEconomics_ReturnsImmediately()
    {
        var client = new FakeOrderClient();
        var sut = Safe(client);
        var filled = Order("p1", "cid", "FILLED", 101m, 2m, 202m);

        var result = await sut.WaitForFillAsync("BTCUSDC", filled, "cid", default);

        Assert.Same(filled, result);
        Assert.Equal(0, client.GetOrderCalls);
        Assert.Equal(0, client.GetTradeFillsCalls);
    }

    [Fact]
    public async Task WaitForFillAsync_FilledOrderWithoutEconomics_RecoversFromTradeFills()
    {
        var incomplete = Order("p1", "cid", "FILLED");
        var client = new FakeOrderClient
        {
            OrderById = incomplete,
            TradeFills =
            [
                Fill("p1", 100m, 1m, 100m),
                Fill("p1", 110m, 2m, 220m)
            ]
        };
        var sut = Safe(client);

        var result = await sut.WaitForFillAsync("BTCUSDC", incomplete, "cid", default);

        Assert.Equal(320m / 3m, result.AveragePrice);
        Assert.Equal(3m, result.ExecutedQuantity);
        Assert.Equal(320m, result.CumulativeQuoteQuantity);
        Assert.Equal(1, client.GetTradeFillsCalls);
    }

    [Fact]
    public async Task SafeCancelNormalAsync_WhenCancelFailsButOrderIsAbsent_ReturnsTrue()
    {
        var client = new FakeOrderClient
        {
            CancelOrderException = new HttpRequestException("unknown cancel outcome"),
            OpenOrders = []
        };
        var sut = Safe(client);

        var result = await sut.SafeCancelNormalAsync("BTCUSDC", "o1", "cid", default);

        Assert.True(result);
        Assert.Equal(1, client.CancelOrderCalls);
        Assert.Equal(1, client.GetOpenOrdersCalls);
    }

    [Fact]
    public async Task SafeCancelNormalAsync_WhenCancelFailsAndOrderStillExists_ReturnsFalse()
    {
        var client = new FakeOrderClient
        {
            CancelOrderException = new HttpRequestException("unknown cancel outcome"),
            OpenOrders = [new BinanceOpenOrder { OrderId = "o1", ClientOrderId = "cid" }]
        };
        var sut = Safe(client);

        var result = await sut.SafeCancelNormalAsync("BTCUSDC", "o1", "cid", default);

        Assert.False(result);
    }

    [Fact]
    public async Task ExchangeInfo_RoundPrice_QuantizesDown()
    {
        var client = new FakeOrderClient
        {
            Filters = new BinanceSymbolFilters { TickSize = 0.1m, StepSize = 0.001m, MinQuantity = 0.001m }
        };
        var sut = Exchange(client);

        var result = await sut.RoundPriceAsync("btcusdc", 100.19m, default);

        Assert.Equal(100.1m, result);
        Assert.Equal(1, client.GetFiltersCalls);
        Assert.Equal("BTCUSDC", client.LastFilterSymbol);
    }

    [Fact]
    public async Task ExchangeInfo_RoundQuantity_BelowMinimum_ReturnsZero()
    {
        var client = new FakeOrderClient
        {
            Filters = new BinanceSymbolFilters { TickSize = 0.1m, StepSize = 0.001m, MinQuantity = 0.005m }
        };
        var sut = Exchange(client);

        var result = await sut.RoundQuantityAsync("BTCUSDC", 0.0049m, default);

        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task ExchangeInfo_UsesCachedFiltersForSameNormalizedSymbol()
    {
        var client = new FakeOrderClient
        {
            Filters = new BinanceSymbolFilters { TickSize = 0.1m, StepSize = 0.001m, MinQuantity = 0.001m }
        };
        var sut = Exchange(client);

        await sut.RoundPriceAsync("btcusdc", 100.19m, default);
        await sut.RoundQuantityAsync(" BTCUSDC ", 0.0109m, default);

        Assert.Equal(1, client.GetFiltersCalls);
    }

    [Fact]
    public async Task TpOnlyOpen_Long_CreatesFullQuantityTakeProfitAtQuantizedDistance()
    {
        var client = new FakeOrderClient
        {
            MarketOrder = Order("parent", "parent-cid", "FILLED", 60_000m, 0.002m, 120m),
            Filters = new BinanceSymbolFilters { TickSize = 0.1m, StepSize = 0.001m, MinQuantity = 0.001m },
            LimitOrder = Order("tp", "tp-cid", "NEW")
        };
        var sut = new BinanceTpOnlyPositionService(
            client,
            Safe(client),
            new TestClock(Now),
            NullLogger<BinanceTpOnlyPositionService>.Instance);

        var result = await sut.OpenAsync("BOT8012", "BTCUSDC", PositionSide.Long, 0.002m, 200.07m, default);

        Assert.Equal(60_000m, result.EntryPrice);
        Assert.Equal(60_200m, result.TpPrice);
        Assert.Equal(0.002m, client.LastLimitQuantity);
        Assert.Equal(60_200m, client.LastLimitPrice);
        Assert.Equal("SELL", client.LastLimitSide);
        Assert.Equal("LONG", client.LastLimitPositionSide);
    }

    [Fact]
    public async Task ProtectedOpen_WhenStopCreationFails_CancelsCreatedTakeProfit()
    {
        var client = new FakeOrderClient
        {
            MarketOrder = Order("parent", "parent-cid", "FILLED", 60_000m, 0.002m, 120m),
            Filters = new BinanceSymbolFilters { TickSize = 0.1m, StepSize = 0.001m, MinQuantity = 0.001m },
            LimitOrder = Order("tp-order", "tp-cid", "NEW"),
            StopAlgoException = new InvalidOperationException("stop failed")
        };
        var sut = new BinanceProtectedPositionService(
            client,
            Safe(client),
            Exchange(client),
            new TestClock(Now),
            NullLogger<BinanceProtectedPositionService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.OpenAsync("BOT8015", "BTCUSDC", PositionSide.Long, 0.002m, 0.12m, 300m, default));

        Assert.Contains("tp-order", client.CancelledOrderIds);
    }

    [Fact]
    public async Task ProtectedOpen_Long_CreatesHalfQuantityTpAndFullQuantityStop()
    {
        var client = new FakeOrderClient
        {
            MarketOrder = Order("parent", "parent-cid", "FILLED", 60_000m, 0.002m, 120m),
            Filters = new BinanceSymbolFilters { TickSize = 0.1m, StepSize = 0.001m, MinQuantity = 0.001m },
            LimitOrder = Order("tp-order", "tp-cid", "NEW"),
            StopAlgoOrder = new BinanceAlgoOrderResult
            {
                Symbol = "BTCUSDC",
                ClientOrderId = "sl-cid",
                AlgoOrderId = "sl-order",
                Status = "NEW"
            }
        };
        var sut = new BinanceProtectedPositionService(
            client,
            Safe(client),
            Exchange(client),
            new TestClock(Now),
            NullLogger<BinanceProtectedPositionService>.Instance);

        var result = await sut.OpenAsync("BOT8015", "BTCUSDC", PositionSide.Long, 0.002m, 0.12m, 300m, default);

        Assert.Equal(60_072m, result.TpPrice);
        Assert.Equal(59_700m, result.SlPrice);
        Assert.Equal(0.001m, client.LastLimitQuantity);
        Assert.Equal(0.002m, client.LastStopQuantity);
        Assert.Equal(59_700m, client.LastStopPrice);
        Assert.True(result.ProtectiveActive);
    }

    private static SafeBinanceOrderService Safe(FakeOrderClient client)
        => new(client, NullLogger<SafeBinanceOrderService>.Instance);

    private static BinanceExchangeInfoService Exchange(FakeOrderClient client)
        => new(
            client,
            Options.Create(new BinanceFuturesOptions
            {
                ExchangeInfoCacheDuration = TimeSpan.FromHours(1)
            }));

    private static BinanceOrderResult Order(
        string orderId,
        string clientId,
        string status,
        decimal? averagePrice = null,
        decimal? executedQuantity = null,
        decimal? quoteQuantity = null)
        => new()
        {
            Symbol = "BTCUSDC",
            ClientOrderId = clientId,
            OrderId = orderId,
            Status = status,
            AveragePrice = averagePrice,
            ExecutedQuantity = executedQuantity,
            CumulativeQuoteQuantity = quoteQuantity
        };

    private static BinanceTradeFill Fill(
        string orderId,
        decimal price,
        decimal quantity,
        decimal quoteQuantity)
        => new()
        {
            Symbol = "BTCUSDC",
            OrderId = orderId,
            Price = price,
            Quantity = quantity,
            QuoteQuantity = quoteQuantity
        };

    private sealed class TestClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class FakeOrderClient : IBinanceFuturesOrderClient
    {
        public BinanceOrderResult MarketOrder { get; set; } = Order("market", "market-cid", "FILLED", 100m, 1m, 100m);
        public BinanceOrderResult LimitOrder { get; set; } = Order("limit", "limit-cid", "NEW");
        public BinanceOrderResult? OrderById { get; set; }
        public BinanceOrderResult? OrderByClientId { get; set; }
        public BinanceAlgoOrderResult StopAlgoOrder { get; set; } = new()
        {
            Symbol = "BTCUSDC",
            ClientOrderId = "algo-cid",
            AlgoOrderId = "algo-1",
            Status = "NEW"
        };
        public BinanceSymbolFilters Filters { get; set; } = new()
        {
            TickSize = 0.1m,
            StepSize = 0.001m,
            MinQuantity = 0.001m
        };
        public IReadOnlyCollection<BinanceTradeFill> TradeFills { get; set; } = [];
        public IReadOnlyCollection<BinanceOpenOrder> OpenOrders { get; set; } = [];
        public IReadOnlyCollection<BinanceOpenAlgoOrder> OpenAlgoOrders { get; set; } = [];

        public Exception? MarketOrderException { get; set; }
        public Exception? CancelOrderException { get; set; }
        public Exception? StopAlgoException { get; set; }

        public int PlaceMarketCalls { get; private set; }
        public int GetOrderCalls { get; private set; }
        public int GetByClientIdCalls { get; private set; }
        public int GetTradeFillsCalls { get; private set; }
        public int CancelOrderCalls { get; private set; }
        public int GetOpenOrdersCalls { get; private set; }
        public int GetFiltersCalls { get; private set; }

        public string? LastFilterSymbol { get; private set; }
        public decimal LastLimitQuantity { get; private set; }
        public decimal LastLimitPrice { get; private set; }
        public string? LastLimitSide { get; private set; }
        public string? LastLimitPositionSide { get; private set; }
        public decimal LastStopQuantity { get; private set; }
        public decimal LastStopPrice { get; private set; }
        public List<string> CancelledOrderIds { get; } = [];

        public Task<BinanceOrderResult> PlaceMarketOrderAsync(
            string symbol,
            string side,
            string positionSide,
            decimal quantity,
            string clientOrderId,
            CancellationToken ct)
        {
            PlaceMarketCalls++;
            if (MarketOrderException is not null)
                throw MarketOrderException;

            return Task.FromResult(MarketOrder with
            {
                Symbol = symbol,
                ClientOrderId = clientOrderId
            });
        }

        public Task<BinanceOrderResult> PlaceLimitOrderAsync(
            string symbol,
            string side,
            string positionSide,
            decimal quantity,
            decimal price,
            string clientOrderId,
            CancellationToken ct)
        {
            LastLimitQuantity = quantity;
            LastLimitPrice = price;
            LastLimitSide = side;
            LastLimitPositionSide = positionSide;

            return Task.FromResult(LimitOrder with
            {
                Symbol = symbol,
                ClientOrderId = clientOrderId
            });
        }

        public Task<BinanceAlgoOrderResult> PlaceTakeProfitMarketAlgoOrderAsync(
            string symbol,
            string side,
            string positionSide,
            decimal quantity,
            decimal stopPrice,
            string clientOrderId,
            CancellationToken ct)
            => Task.FromResult(StopAlgoOrder);

        public Task<BinanceAlgoOrderResult> PlaceStopMarketAlgoOrderAsync(
            string symbol,
            string side,
            string positionSide,
            decimal quantity,
            decimal stopPrice,
            string clientOrderId,
            CancellationToken ct)
        {
            LastStopQuantity = quantity;
            LastStopPrice = stopPrice;

            if (StopAlgoException is not null)
                throw StopAlgoException;

            return Task.FromResult(StopAlgoOrder with
            {
                Symbol = symbol,
                ClientOrderId = clientOrderId
            });
        }

        public Task<BinanceOrderResult> GetOrderAsync(string symbol, string orderId, CancellationToken ct)
        {
            GetOrderCalls++;
            return Task.FromResult(OrderById ?? MarketOrder);
        }

        public Task<BinanceOrderResult> GetOrderByClientOrderIdAsync(string symbol, string clientOrderId, CancellationToken ct)
        {
            GetByClientIdCalls++;
            return Task.FromResult(OrderByClientId ?? MarketOrder with { ClientOrderId = clientOrderId });
        }

        public Task CancelOrderAsync(string symbol, string orderId, CancellationToken ct)
        {
            CancelOrderCalls++;
            if (CancelOrderException is not null)
                throw CancelOrderException;

            CancelledOrderIds.Add(orderId);
            return Task.CompletedTask;
        }

        public Task CancelAlgoOrderAsync(string symbol, string algoOrderId, CancellationToken ct)
            => Task.CompletedTask;

        public Task<IReadOnlyCollection<BinanceOpenOrder>> GetOpenOrdersAsync(string symbol, CancellationToken ct)
        {
            GetOpenOrdersCalls++;
            return Task.FromResult(OpenOrders);
        }

        public Task<IReadOnlyCollection<BinanceOpenAlgoOrder>> GetOpenAlgoOrdersAsync(string symbol, CancellationToken ct)
            => Task.FromResult(OpenAlgoOrders);

        public Task<BinanceSymbolFilters> GetSymbolFiltersAsync(string symbol, CancellationToken ct)
        {
            GetFiltersCalls++;
            LastFilterSymbol = symbol;
            return Task.FromResult(Filters);
        }

        public Task SetHedgeModeAsync(CancellationToken ct) => Task.CompletedTask;

        public Task SetLeverageAsync(string symbol, int leverage, CancellationToken ct)
            => Task.CompletedTask;

        public Task<IReadOnlyCollection<BinancePositionRisk>> GetPositionRiskAsync(string symbol, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<BinancePositionRisk>>([]);

        public Task<decimal> GetMarkPriceAsync(string symbol, CancellationToken ct)
            => Task.FromResult(100m);

        public Task<IReadOnlyCollection<BinanceTradeFill>> GetTradeFillsForOrderAsync(
            string symbol,
            string orderId,
            CancellationToken ct)
        {
            GetTradeFillsCalls++;
            return Task.FromResult(TradeFills);
        }
    }
}
