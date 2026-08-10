using TradingSystem.Binance.Execution.Models;
using Xunit;

namespace TradingSystem.Binance.Tests;

public sealed class BinanceClientOrderIdTests
{
    [Fact]
    public void NewShortId_ReturnsEightCharacters()
    {
        Assert.Equal(8, BinanceClientOrderId.NewShortId().Length);
    }

    [Fact]
    public void Create_NormalizesBotAndRole()
    {
        var result = BinanceClientOrderId.Create("bot-8012", "tp", "abc123");
        Assert.Equal("BOT8012_TP_abc123", result);
    }

    [Fact]
    public void Create_WithSequence_AppendsSequence()
    {
        var result = BinanceClientOrderId.Create("BOT8012", "S3", "abc", 4);
        Assert.EndsWith("_T4", result);
    }

    [Fact]
    public void Create_LongValue_IsLimitedTo32Characters()
    {
        var result = BinanceClientOrderId.Create("VERYLONGBOTNAME123456789", "TAKEPROFIT", "abcdefghijklmnop", 999);
        Assert.True(result.Length <= 32);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("BOT_ONLY")]
    public void TryParse_InvalidValue_ReturnsFalse(string? value)
    {
        Assert.False(BinanceClientOrderId.TryParse(value, out _, out _, out _));
    }

    [Fact]
    public void TryParse_ValidValue_ParsesFirstThreeParts()
    {
        var result = BinanceClientOrderId.TryParse("bot8012_tp_abc_T2", out var bot, out var role, out var id);

        Assert.True(result);
        Assert.Equal("BOT8012", bot);
        Assert.Equal("TP", role);
        Assert.Equal("abc", id);
    }
}
