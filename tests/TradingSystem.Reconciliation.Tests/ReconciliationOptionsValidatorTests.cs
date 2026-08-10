using TradingSystem.Reconciliation.Configuration;
using Xunit;

namespace TradingSystem.Reconciliation.Tests;

public sealed class ReconciliationOptionsValidatorTests
{
    private readonly ReconciliationOptionsValidator _sut = new();

    [Theory]
    [InlineData(4)]
    [InlineData(86401)]
    public void Validate_IntervalOutsideRange_Fails(int value)
    {
        var options = new ReconciliationOptions { IntervalSeconds = value };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_NegativeQuantityTolerance_Fails()
    {
        var options = new ReconciliationOptions { QuantityTolerance = -1m };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_EmptyBots_Fails()
    {
        var options = new ReconciliationOptions { Bots = [] };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_WhitespaceBot_Fails()
    {
        var options = new ReconciliationOptions { Bots = ["BOT1", " "] };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_EmptySymbols_Fails()
    {
        var options = new ReconciliationOptions { Symbols = [] };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_DuplicateBots_FailsCaseInsensitive()
    {
        var options = new ReconciliationOptions { Bots = ["BOT1", "bot1"] };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_DuplicateSymbols_FailsCaseInsensitive()
    {
        var options = new ReconciliationOptions { Symbols = ["BTCUSDC", "btcusdc"] };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }
}
