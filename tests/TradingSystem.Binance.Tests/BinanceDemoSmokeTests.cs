using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingSystem.Binance;
using TradingSystem.Binance.Orders.Contracts;
using Xunit;

namespace TradingSystem.Binance.Tests;

public sealed class BinanceDemoSmokeTests
{
    [Fact]
    public async Task Demo_ReadOnlySignedEndpoints_ShouldWork()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (currentDirectory is not null &&
               !File.Exists(Path.Combine(currentDirectory.FullName, "BinanceTradingSystem.sln")))
        {
            currentDirectory = currentDirectory.Parent;
        }

        Assert.NotNull(currentDirectory);

        var appsettingsPath = Path.Combine(
            currentDirectory!.FullName,
            "src",
            "StrategyService",
            "appsettings.json");

        Assert.True(
            File.Exists(appsettingsPath),
            $"Could not find StrategyService appsettings.json at '{appsettingsPath}'.");

        using var document = JsonDocument.Parse(
            await File.ReadAllTextAsync(appsettingsPath));

        var root = document.RootElement;

        var environmentName =
            root.GetProperty("TradingEnvironment")
                .GetProperty("EnvironmentName")
                .GetString();

        var binance = root.GetProperty("BinanceFutures");

        var values = new Dictionary<string, string?>
        {
            ["TradingEnvironment:EnvironmentName"] = environmentName,
            ["BinanceFutures:BaseUrl"] = binance.GetProperty("BaseUrl").GetString(),
            ["BinanceFutures:ApiKey"] = binance.GetProperty("ApiKey").GetString(),
            ["BinanceFutures:SecretKey"] = binance.GetProperty("SecretKey").GetString(),
            ["BinanceFutures:ReceiveWindow"] = binance.GetProperty("ReceiveWindow").ToString(),
            ["BinanceFutures:RequireSignedOperations"] =
                binance.GetProperty("RequireSignedOperations").ToString()
        };

        if (binance.TryGetProperty("ExchangeInfoCacheDuration", out var cacheDuration))
        {
            values["BinanceFutures:ExchangeInfoCacheDuration"] =
                cacheDuration.GetString();
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddBinanceFutures(configuration);

        await using var provider = services.BuildServiceProvider();

        var client =
            provider.GetRequiredService<IBinanceFuturesOrderClient>();

        var positions =
            await client.GetPositionRiskAsync(
                "BTCUSDC",
                default);

        var orders =
            await client.GetOpenOrdersAsync(
                "BTCUSDC",
                default);

        var algoOrders =
            await client.GetOpenAlgoOrdersAsync(
                "BTCUSDC",
                default);

        Assert.NotNull(positions);
        Assert.NotNull(orders);
        Assert.NotNull(algoOrders);

        Console.WriteLine($"Position risk rows: {positions.Count}");
        Console.WriteLine($"Open orders: {orders.Count}");
        Console.WriteLine($"Open algo orders: {algoOrders.Count}");
    }
    [Fact]
    public async Task Demo_CurrentAccountState_ShouldBeReadable()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (currentDirectory is not null &&
               !File.Exists(Path.Combine(currentDirectory.FullName, "BinanceTradingSystem.sln")))
        {
            currentDirectory = currentDirectory.Parent;
        }

        Assert.NotNull(currentDirectory);

        var appsettingsPath = Path.Combine(
            currentDirectory!.FullName,
            "src",
            "StrategyService",
            "appsettings.json");

        using var document = JsonDocument.Parse(
            await File.ReadAllTextAsync(appsettingsPath));

        var binance = document.RootElement.GetProperty("BinanceFutures");

        var values = new Dictionary<string, string?>
        {
            ["BinanceFutures:BaseUrl"] = binance.GetProperty("BaseUrl").GetString(),
            ["BinanceFutures:ApiKey"] = binance.GetProperty("ApiKey").GetString(),
            ["BinanceFutures:SecretKey"] = binance.GetProperty("SecretKey").GetString(),
            ["BinanceFutures:ReceiveWindow"] = binance.GetProperty("ReceiveWindow").ToString(),
            ["BinanceFutures:RequireSignedOperations"] =
                binance.GetProperty("RequireSignedOperations").ToString()
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBinanceFutures(configuration);

        await using var provider = services.BuildServiceProvider();

        var client =
            provider.GetRequiredService<IBinanceFuturesOrderClient>();

        var positions = await client.GetPositionRiskAsync("BTCUSDC", default);
        var orders = await client.GetOpenOrdersAsync("BTCUSDC", default);
        var algoOrders = await client.GetOpenAlgoOrdersAsync("BTCUSDC", default);

        Console.WriteLine($"Position risk rows: {positions.Count}");
        Console.WriteLine($"Open orders: {orders.Count}");
        Console.WriteLine($"Open algo orders: {algoOrders.Count}");

        foreach (var position in positions)
        {
            Console.WriteLine(
                $"Position: Symbol={position.Symbol}");
        }

        Assert.Empty(orders);
        Assert.Empty(algoOrders);
    }

    [Fact]
    public async Task Demo_HedgeModeAndLeverage_ShouldBeAccepted()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (currentDirectory is not null &&
               !File.Exists(Path.Combine(currentDirectory.FullName, "BinanceTradingSystem.sln")))
        {
            currentDirectory = currentDirectory.Parent;
        }

        Assert.NotNull(currentDirectory);

        var appsettingsPath = Path.Combine(
            currentDirectory!.FullName,
            "src",
            "StrategyService",
            "appsettings.json");

        using var document = JsonDocument.Parse(
            await File.ReadAllTextAsync(appsettingsPath));

        var binance = document.RootElement.GetProperty("BinanceFutures");

        var values = new Dictionary<string, string?>
        {
            ["BinanceFutures:BaseUrl"] = binance.GetProperty("BaseUrl").GetString(),
            ["BinanceFutures:ApiKey"] = binance.GetProperty("ApiKey").GetString(),
            ["BinanceFutures:SecretKey"] = binance.GetProperty("SecretKey").GetString(),
            ["BinanceFutures:ReceiveWindow"] = binance.GetProperty("ReceiveWindow").ToString(),
            ["BinanceFutures:RequireSignedOperations"] =
                binance.GetProperty("RequireSignedOperations").ToString()
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBinanceFutures(configuration);

        await using var provider = services.BuildServiceProvider();

        var client =
            provider.GetRequiredService<IBinanceFuturesOrderClient>();

        await client.SetHedgeModeAsync(default);

        await client.SetLeverageAsync(
            "BTCUSDC",
            50,
            default);
    }
}