using TradingSystem.Indicators.Alligator.Configuration;
using TradingSystem.Indicators.Bollinger.Configuration;
using Xunit;

namespace TradingSystem.Indicators.Tests;

public sealed class IndicatorOptionsValidatorTests
{
    [Fact]
    public void Alligator_DefaultOptions_Succeed()
    {
        Assert.True(new AlligatorOptionsValidator().Validate(null, new AlligatorOptions()).Succeeded);
    }

    [Fact]
    public void Alligator_EmptySymbols_Fail()
    {
        var options = new AlligatorOptions { Symbols = [] };
        Assert.False(new AlligatorOptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Alligator_EmptyIntervals_Fail()
    {
        var options = new AlligatorOptions { Intervals = [] };
        Assert.False(new AlligatorOptionsValidator().Validate(null, options).Succeeded);
    }

    [Theory]
    [InlineData("sma")]
    [InlineData("jaw")]
    [InlineData("teeth")]
    [InlineData("lips")]
    [InlineData("history")]
    public void Alligator_NonPositiveLengths_Fail(string property)
    {
        var options = new AlligatorOptions();
        if (property == "sma") options.SmaLength = 0;
        if (property == "jaw") options.JawLength = 0;
        if (property == "teeth") options.TeethLength = 0;
        if (property == "lips") options.LipsLength = 0;
        if (property == "history") options.HistoryLimit = 0;

        Assert.False(new AlligatorOptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Alligator_HistoryBelowSma_Fails()
    {
        var options = new AlligatorOptions { HistoryLimit = 10, SmaLength = 20 };
        Assert.False(new AlligatorOptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Bollinger_ValidOptions_Succeed()
    {
        Assert.True(new BollingerOptionsValidator().Validate(null, ValidBollinger()).Succeeded);
    }

    [Fact]
    public void Bollinger_RequiresSymbolsIntervalsAndBands()
    {
        var options = ValidBollinger();
        options.Symbols = [];
        options.Intervals = [];
        options.Bands = [];

        var result = new BollingerOptionsValidator().Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Bollinger_InvalidBandValues_Fail()
    {
        var options = ValidBollinger();
        options.Bands = [new BollingerBandOptions { Name = "", Length = 0, Multiplier = 0, Source = "median" }];

        Assert.False(new BollingerOptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Bollinger_DuplicateBandNames_FailCaseInsensitive()
    {
        var options = ValidBollinger();
        options.Bands = [Band("fast", 10), Band("FAST", 20)];

        Assert.False(new BollingerOptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Bollinger_HistoryBelowLongestBand_Fails()
    {
        var options = ValidBollinger();
        options.HistoryLimit = 10;
        options.Bands = [Band("slow", 20)];

        Assert.False(new BollingerOptionsValidator().Validate(null, options).Succeeded);
    }

    private static BollingerOptions ValidBollinger() => new() { Symbols = ["BTCUSDC"], Intervals = ["1m"], HistoryLimit = 100, Bands = [Band("bb20", 20)] };
    private static BollingerBandOptions Band(string name, int length) => new() { Name = name, Length = length, Source = "close", Multiplier = 2m };
}
