using TradingSystem.Signals.Configuration;
using Xunit;

namespace TradingSystem.Signals.Tests;

public sealed class SignalGenerationOptionsValidatorTests
{
    private readonly SignalGenerationOptionsValidator _sut = new();

    [Fact]
    public void Validate_EmptyDictionary_Succeeds()
    {
        Assert.True(_sut.Validate(null, new SignalGenerationOptions()).Succeeded);
    }

    [Fact]
    public void Validate_NullBots_Fails()
    {
        var options = new SignalGenerationOptions { Bots = null! };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_EnabledBotWithEmptySymbol_Fails()
    {
        var options = Options(new BotSignalModeOptions { Enabled = true, Symbol = "", Interval = "1m" });
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_EnabledBotWithEmptyInterval_Fails()
    {
        var options = Options(new BotSignalModeOptions { Enabled = true, Symbol = "BTCUSDC", Interval = "" });
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(86401)]
    public void Validate_ThrottleOutsideRange_Fails(int value)
    {
        var options = Options(new BotSignalModeOptions { Enabled = true, Symbol = "BTCUSDC", Interval = "1m", MinimumSecondsBetweenGeneratedSignals = value });
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_DisabledBotDoesNotRequireSymbolOrInterval()
    {
        var options = Options(new BotSignalModeOptions { Enabled = false, Symbol = "", Interval = "" });
        Assert.True(_sut.Validate(null, options).Succeeded);
    }

    private static SignalGenerationOptions Options(BotSignalModeOptions bot) => new() { Bots = new Dictionary<string, BotSignalModeOptions>(StringComparer.OrdinalIgnoreCase) { ["BOT8012"] = bot } };
}
