using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Binance;
using TradingSystem.Binance.Exceptions;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Orders.Models;
using Xunit;

namespace TradingSystem.Binance.Tests;

public sealed class BinanceDemoSmokeTests
{
    private const string Symbol = "BTCUSDC";

    [Fact]
    public async Task Demo_Configuration_ShouldPointToDemoEnvironment()
    {
        using var document = await LoadStrategySettingsAsync();

        var root = document.RootElement;
        var binance = root.GetProperty("BinanceFutures");

        var environment = root
            .GetProperty("TradingEnvironment")
            .GetProperty("EnvironmentName")
            .GetString();

        var baseUrl = binance
            .GetProperty("BaseUrl")
            .GetString();

        var apiKey = binance
            .GetProperty("ApiKey")
            .GetString();

        var secret = binance
            .GetProperty("SecretKey")
            .GetString();

        Assert.Equal("Paper", environment);
        Assert.Equal(
            "https://demo-fapi.binance.com",
            baseUrl?.TrimEnd('/'));

        Assert.False(string.IsNullOrWhiteSpace(apiKey));
        Assert.False(string.IsNullOrWhiteSpace(secret));

        Assert.True(
            binance.GetProperty("RequireSignedOperations")
                .GetBoolean());
    }

    [Fact]
    public async Task Demo_PublicMarketData_ShouldBeReadable()
    {
        await using var provider = await CreateProviderAsync();

        var client =
            provider.GetRequiredService<IBinanceFuturesOrderClient>();

        var markPrice =
            await client.GetMarkPriceAsync(
                Symbol,
                default);

        var filters =
            await client.GetSymbolFiltersAsync(
                Symbol,
                default);

        Assert.True(markPrice > 0);
        Assert.True(filters.TickSize > 0);
        Assert.True(filters.StepSize > 0);
        Assert.True(filters.MinQuantity > 0);

        Console.WriteLine($"Mark price: {markPrice}");
        Console.WriteLine($"Tick size: {filters.TickSize}");
        Console.WriteLine($"Step size: {filters.StepSize}");
        Console.WriteLine($"Min quantity: {filters.MinQuantity}");
    }

    [Fact]
    public async Task Demo_ReadOnlySignedEndpoints_ShouldWork()
    {
        await using var provider = await CreateProviderAsync();

        var client =
            provider.GetRequiredService<IBinanceFuturesOrderClient>();

        var positions =
            await client.GetPositionRiskAsync(
                Symbol,
                default);

        var orders =
            await client.GetOpenOrdersAsync(
                Symbol,
                default);

        var algoOrders =
            await client.GetOpenAlgoOrdersAsync(
                Symbol,
                default);

        Assert.NotNull(positions);
        Assert.NotNull(orders);
        Assert.NotNull(algoOrders);

        Console.WriteLine(
            $"Position risk rows: {positions.Count}");

        Console.WriteLine(
            $"Open orders: {orders.Count}");

        Console.WriteLine(
            $"Open algo orders: {algoOrders.Count}");
    }

    [Fact]
    public async Task Demo_CurrentAccountState_ShouldBeReadable()
    {
        await using var provider = await CreateProviderAsync();

        var client =
            provider.GetRequiredService<IBinanceFuturesOrderClient>();

        var positions =
            await client.GetPositionRiskAsync(
                Symbol,
                default);

        var orders =
            await client.GetOpenOrdersAsync(
                Symbol,
                default);

        var algoOrders =
            await client.GetOpenAlgoOrdersAsync(
                Symbol,
                default);

        foreach (var position in positions)
        {
            Console.WriteLine(
                $"Position: " +
                $"Symbol={position.Symbol}, " +
                $"Side={position.PositionSide}, " +
                $"Amount={position.PositionAmount}, " +
                $"Entry={position.EntryPrice}, " +
                $"Mark={position.MarkPrice}");
        }

        foreach (var order in orders)
        {
            Console.WriteLine(
                $"Order: " +
                $"Id={order.OrderId}, " +
                $"ClientId={order.ClientOrderId}, " +
                $"Side={order.Side}, " +
                $"PositionSide={order.PositionSide}, " +
                $"Price={order.Price}, " +
                $"Quantity={order.Quantity}");
        }

        foreach (var order in algoOrders)
        {
            Console.WriteLine(
                $"Algo: " +
                $"Id={order.AlgoOrderId}, " +
                $"ClientId={order.ClientAlgoId}, " +
                $"Status={order.Status}, " +
                $"Trigger={order.TriggerPrice}");
        }

        Assert.NotNull(positions);
        Assert.NotNull(orders);
        Assert.NotNull(algoOrders);
    }

    [Fact]
    public async Task Demo_HedgeModeAndLeverage_ShouldBeAccepted()
    {
        await using var provider = await CreateProviderAsync();

        var client =
            provider.GetRequiredService<IBinanceFuturesOrderClient>();

        try
        {
            await client.SetHedgeModeAsync(default);
        }
        catch (BinanceApiException exception)
            when (IsAlreadyInHedgeMode(exception))
        {
            Console.WriteLine(
                "Hedge mode is already enabled.");
        }

        await client.SetLeverageAsync(
            Symbol,
            49,
            default);
    }

    [Fact]
    public async Task Demo_LimitOrder_CreateReadCancel_ShouldWork()
    {
        await using var provider = await CreateProviderAsync();

        var client =
            provider.GetRequiredService<IBinanceFuturesOrderClient>();

        var markPrice =
            await client.GetMarkPriceAsync(
                Symbol,
                default);

        Assert.True(
            markPrice > 0,
            "Could not obtain Demo mark price.");

        var filters =
            await client.GetSymbolFiltersAsync(
                Symbol,
                default);

        Assert.True(filters.TickSize > 0);
        Assert.True(filters.StepSize > 0);

        var desiredPrice =
            markPrice * 0.98m;

        var price =
            QuantizeDown(
                desiredPrice,
                filters.TickSize);

        var requestedQuantity =
            Math.Max(
                0.002m,
                filters.MinQuantity);

        var quantity =
            QuantizeDown(
                requestedQuantity,
                filters.StepSize);

        Assert.True(price > 0);
        Assert.True(quantity >= filters.MinQuantity);

        var shortId =
            $"d{Guid.NewGuid():N}"[..9];

        var clientOrderId =
            $"BOT8014_ENTRY_{shortId}";

        BinanceOrderResult? created = null;

        try
        {
            Console.WriteLine(
                $"Mark price: {markPrice}");

            Console.WriteLine(
                $"Limit price: {price}");

            Console.WriteLine(
                $"Quantity: {quantity}");

            Console.WriteLine(
                $"ClientOrderId: {clientOrderId}");

            created =
                await client.PlaceLimitOrderAsync(
                    Symbol,
                    "BUY",
                    "LONG",
                    quantity,
                    price,
                    clientOrderId,
                    default);

            Assert.False(
                string.IsNullOrWhiteSpace(created.OrderId));

            Assert.Equal(
                clientOrderId,
                created.ClientOrderId);

            Console.WriteLine(
                $"OrderId: {created.OrderId}");

            Console.WriteLine(
                $"Create status: {created.Status}");

            var loaded =
                await client.GetOrderAsync(
                    Symbol,
                    created.OrderId,
                    default);

            Assert.Equal(
                created.OrderId,
                loaded.OrderId);

            Assert.Equal(
                clientOrderId,
                loaded.ClientOrderId);

            var openOrders =
                await client.GetOpenOrdersAsync(
                    Symbol,
                    default);

            Assert.Contains(
                openOrders,
                x => x.OrderId == created.OrderId);

            await client.CancelOrderAsync(
                Symbol,
                created.OrderId,
                default);

            await Task.Delay(
                TimeSpan.FromSeconds(2));

            var afterCancel =
                await client.GetOpenOrdersAsync(
                    Symbol,
                    default);

            Assert.DoesNotContain(
                afterCancel,
                x => x.OrderId == created.OrderId);

            var cancelled =
                await client.GetOrderAsync(
                    Symbol,
                    created.OrderId,
                    default);

            Assert.Equal(
                "CANCELED",
                cancelled.Status,
                ignoreCase: true);

            Console.WriteLine(
                $"Final status: {cancelled.Status}");
        }
        finally
        {
            if (created is not null &&
                !string.IsNullOrWhiteSpace(created.OrderId))
            {
                await TryCancelAsync(
                    client,
                    created.OrderId);
            }
        }
    }

    [Fact]
    public async Task Demo_LimitOrder_Cancel_ShouldLeaveNoOpenOrder()
    {
        await using var provider = await CreateProviderAsync();

        var client =
            provider.GetRequiredService<IBinanceFuturesOrderClient>();

        var markPrice =
            await client.GetMarkPriceAsync(
                Symbol,
                default);

        var filters =
            await client.GetSymbolFiltersAsync(
                Symbol,
                default);

        var price =
            QuantizeDown(
                markPrice * 0.98m,
                filters.TickSize);

        var quantity =
            QuantizeDown(
                Math.Max(
                    0.002m,
                    filters.MinQuantity),
                filters.StepSize);

        var shortId =
            $"c{Guid.NewGuid():N}"[..9];

        var clientOrderId =
            $"BOT8014_ENTRY_{shortId}";

        BinanceOrderResult? created = null;

        try
        {
            created =
                await client.PlaceLimitOrderAsync(
                    Symbol,
                    "BUY",
                    "LONG",
                    quantity,
                    price,
                    clientOrderId,
                    default);

            await client.CancelOrderAsync(
                Symbol,
                created.OrderId,
                default);

            await Task.Delay(
                TimeSpan.FromSeconds(1));

            var orders =
                await client.GetOpenOrdersAsync(
                    Symbol,
                    default);

            Assert.DoesNotContain(
                orders,
                x =>
                    x.OrderId == created.OrderId ||
                    x.ClientOrderId == clientOrderId);
        }
        finally
        {
            if (created is not null)
            {
                await TryCancelAsync(
                    client,
                    created.OrderId);
            }
        }
    }

    [Fact]
    public async Task Demo_LimitOrder_ShouldNotCreatePositionBeforeFill()
    {
        await using var provider = await CreateProviderAsync();

        var client =
            provider.GetRequiredService<IBinanceFuturesOrderClient>();

        var before =
            await GetLongPositionAmountAsync(
                client);

        var markPrice =
            await client.GetMarkPriceAsync(
                Symbol,
                default);

        var filters =
            await client.GetSymbolFiltersAsync(
                Symbol,
                default);

        var price =
            QuantizeDown(
                markPrice * 0.98m,
                filters.TickSize);

        var quantity =
            QuantizeDown(
                Math.Max(
                    0.002m,
                    filters.MinQuantity),
                filters.StepSize);

        var shortId =
            $"p{Guid.NewGuid():N}"[..9];

        var clientOrderId =
            $"BOT8014_ENTRY_{shortId}";

        BinanceOrderResult? created = null;

        try
        {
            created =
                await client.PlaceLimitOrderAsync(
                    Symbol,
                    "BUY",
                    "LONG",
                    quantity,
                    price,
                    clientOrderId,
                    default);

            await Task.Delay(
                TimeSpan.FromSeconds(1));

            var after =
                await GetLongPositionAmountAsync(
                    client);

            Assert.Equal(
                before,
                after);
        }
        finally
        {
            if (created is not null)
            {
                await TryCancelAsync(
                    client,
                    created.OrderId);
            }
        }
    }

    private static async Task<ServiceProvider>
        CreateProviderAsync()
    {
        using var document =
            await LoadStrategySettingsAsync();

        var root =
            document.RootElement;

        var binance =
            root.GetProperty("BinanceFutures");

        var values =
            new Dictionary<string, string?>
            {
                ["BinanceFutures:BaseUrl"] =
                    binance.GetProperty("BaseUrl")
                        .GetString(),

                ["BinanceFutures:ApiKey"] =
                    binance.GetProperty("ApiKey")
                        .GetString(),

                ["BinanceFutures:SecretKey"] =
                    binance.GetProperty("SecretKey")
                        .GetString(),

                ["BinanceFutures:ReceiveWindow"] =
                    binance.GetProperty("ReceiveWindow")
                        .ToString(),

                ["BinanceFutures:RequireSignedOperations"] =
                    "true"
            };

        if (binance.TryGetProperty(
                "ExchangeInfoCacheDuration",
                out var cacheDuration))
        {
            values[
                "BinanceFutures:ExchangeInfoCacheDuration"] =
                cacheDuration.GetString();
        }

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

        var services =
            new ServiceCollection();

        services.AddLogging();
        services.AddBinanceFutures(configuration);

        return services.BuildServiceProvider();
    }

    private static async Task<JsonDocument>
        LoadStrategySettingsAsync()
    {
        var repositoryRoot =
            FindRepositoryRoot();

        var path =
            Path.Combine(
                repositoryRoot,
                "src",
                "StrategyService",
                "appsettings.json");

        Assert.True(
            File.Exists(path),
            $"Could not find StrategyService appsettings.json at '{path}'.");

        return JsonDocument.Parse(
            await File.ReadAllTextAsync(path));
    }

    private static string FindRepositoryRoot()
    {
        var current =
            new DirectoryInfo(
                Directory.GetCurrentDirectory());

        while (current is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        current.FullName,
                        "BinanceTradingSystem.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find BinanceTradingSystem.sln.");
    }

    private static bool IsAlreadyInHedgeMode(
        BinanceApiException exception)
    {
        return exception.Message.Contains(
                   "-4059",
                   StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains(
                   "No need to change position side",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static decimal QuantizeDown(
        decimal value,
        decimal step)
    {
        return step <= 0
            ? value
            : Math.Floor(value / step) * step;
    }

    private static async Task<decimal>
        GetLongPositionAmountAsync(
            IBinanceFuturesOrderClient client)
    {
        var positions =
            await client.GetPositionRiskAsync(
                Symbol,
                default);

        return positions
            .Where(x =>
                x.PositionSide.Equals(
                    "LONG",
                    StringComparison.OrdinalIgnoreCase))
            .Select(x =>
                Math.Abs(x.PositionAmount))
            .FirstOrDefault();
    }

    private static async Task TryCancelAsync(
        IBinanceFuturesOrderClient client,
        string orderId)
    {
        try
        {
            var openOrders =
                await client.GetOpenOrdersAsync(
                    Symbol,
                    default);

            if (openOrders.Any(
                    x => x.OrderId == orderId))
            {
                await client.CancelOrderAsync(
                    Symbol,
                    orderId,
                    default);
            }
        }
        catch
        {
            // Cleanup must not hide the original failure.
        }
    }
}