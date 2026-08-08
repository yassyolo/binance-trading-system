using Microsoft.AspNetCore.Mvc;
using Moq;
using TradingSystem.Dashboard.Api.Controllers;
using TradingSystem.Dashboard.Api.Tests.Infrastructure;
using TradingSystem.Dashboard.Application.Contracts;
using TradingSystem.Dashboard.Application.Models;
using TradingSystem.Dashboard.Contracts.Models.Analytics;
using TradingSystem.Dashboard.Contracts.Models.Charts;
using Xunit;

namespace TradingSystem.Dashboard.Api.Tests.Controllers;

public sealed class ReadControllersTests
{
    [Fact]
    public async Task Signals_ShouldBuildExpectedDashboardQuery()
    {
        var store = new Mock<IDashboardQueryStore>();
        DashboardQuery? captured = null;

        store
            .Setup(x => x.GetSignalsAsync(
                It.IsAny<DashboardQuery>(),
                It.IsAny<CancellationToken>()))
            .Callback<DashboardQuery, CancellationToken>(
                (query, _) => captured = query)
            .ReturnsAsync([]);

        var controller =
            new SignalsController(store.Object)
                .WithUser(role: "Viewer");

        var result = await controller.GetAsync(
            "BOT8012",
            "BTCUSDC",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            10,
            25,
            default);

        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(captured);
        Assert.Equal(10, captured!.Skip);
        Assert.Equal(25, captured.Take);
        Assert.Equal("BOT8012", captured.BotName);
        Assert.Equal("BTCUSDC", captured.Symbol);
    }

    [Fact]
    public async Task Chart_ShouldForwardValidatedArguments()
    {
        var store = new Mock<IDashboardQueryStore>();

        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;

        store
            .Setup(x => x.GetPriceChartAsync(
                "BTCUSDC",
                "5m",
                from,
                to,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PriceChartDto(
                    [],
                    []));

        var controller =
            new ChartsController(store.Object)
                .WithUser(role: "Viewer");

        var result =
            await controller.GetAsync(
                "BTCUSDC",
                "5m",
                from,
                to,
                default);

        Assert.IsType<OkObjectResult>(result);

        store.Verify(
            x => x.GetPriceChartAsync(
                "BTCUSDC",
                "5m",
                from,
                to,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Comparison_WhenStoreReturnsNull_ShouldReturn404()
    {
        var store = new Mock<IDashboardQueryStore>();

        store
            .Setup(x => x.CompareRunsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (StrategyComparisonDto?)null);

        var controller =
            new AnalyticsController(store.Object)
                .WithUser(role: "Viewer");

        var result =
            await controller.CompareRunsAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                default);

        Assert.IsType<NotFoundResult>(result);
    }
}
