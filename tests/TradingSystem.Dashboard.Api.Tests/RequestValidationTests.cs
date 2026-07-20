using TradingSystem.Dashboard.Api.Hardening;
using TradingSystem.Dashboard.Contracts;

namespace TradingSystem.Dashboard.Api.Tests;

public sealed class RequestValidationTests
{
    [Fact]
    public void DangerousCommand_WithoutConfirmation_IsRejected()
    {
        var request = new BotCommandRequest(BotCommandType.EmergencyStop, "Risk incident", false);
        Assert.Throws<ApiValidationException>(() => RequestValidation.Validate(request));
    }

    [Fact]
    public void Pagination_AboveMaximum_IsRejected()
    {
        Assert.Throws<ApiValidationException>(() => RequestValidation.PageSize(501));
    }

    [Fact]
    public void ValidBacktest_IsAccepted()
    {
        var request = new BacktestRequest("BOT8012", "BTCUSDC", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow,
            10_000m, "Internal", 0.04m, 0.01m, new Dictionary<string, string>());
        RequestValidation.Validate(request);
    }
}
