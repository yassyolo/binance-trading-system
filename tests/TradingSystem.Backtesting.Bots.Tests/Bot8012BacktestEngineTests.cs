using TradingSystem.Backtesting.Bots.Bot8012;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Backtesting.Bots.Models.Enums;
using TradingSystem.Domain.MarketData;
using Xunit;

namespace TradingSystem.Backtesting.Bots.Tests;

public sealed class Bot8012BacktestEngineTests
{
    [Fact]
    public void Run_ShouldCloseLongAtTakeProfit()
    {
        var candles = new[]
        {
            new MarketCandle("BTCUSDC", "1m", Utc(0), Utc(1), 100m, 101m, 99m, 100m, 1m),
            new MarketCandle("BTCUSDC", "1m", Utc(1), Utc(2), 100m, 111m, 99m, 110m, 1m)
        };
        var signals = new[] { new HistoricalBotSignal(Utc(0), TradeSide.Long) };
        var result = new Bot8012BacktestEngine().Run(candles, signals, new Bot8012BacktestOptions { ProfitDistance = 10m, PriceDistance = 20m, CooldownSeconds = 0 });
        Assert.Single(result.Positions);
        Assert.Equal("TAKE_PROFIT", result.Positions[0].ExitReason);
    }

    private static DateTime Utc(int minute) => new(2026, 1, 1, 0, minute, 0, DateTimeKind.Utc);
}
