using Moq;
using System.Net;
using TradingSystem.Dashboard.Api.Tests.Infrastructure;
using TradingSystem.Dashboard.Application.Models;
using Xunit;

namespace TradingSystem.Dashboard.Api.Tests.Integration;

public sealed class HttpReadEndpointTests
{
    [Fact]
    public async Task Signals_ShouldMapPaginationAndFiltersToStore()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        var response = await client.GetAsync(
            "/api/v1/signals?botName=BOT8012&symbol=BTCUSDC&skip=5&take=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        factory.QueryStore.Verify(
            x => x.GetSignalsAsync(
                It.Is<DashboardQuery>(
                    q =>
                        q.Skip == 5 &&
                        q.Take == 20 &&
                        q.BotName == "BOT8012" &&
                        q.Symbol == "BTCUSDC"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Positions_ShouldMapStatusToStore()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        var response = await client.GetAsync(
            "/api/v1/positions?botName=BOT8012&status=Open&skip=0&take=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        factory.QueryStore.Verify(
            x => x.GetPositionsAsync(
                It.Is<DashboardQuery>(
                    q =>
                        q.BotName == "BOT8012" &&
                        q.Status == "Open" &&
                        q.Take == 10),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Commands_ZeroTake_ShouldUseDefault100()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        var response = await client.GetAsync(
            "/api/v1/commands?botName=BOT8012&take=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        factory.BotCommands.Verify(
            x => x.GetAsync(
                "BOT8012",
                100,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReplayNotFound_ShouldReturn404()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        var id = Guid.NewGuid();

        var response = await client.GetAsync(
            $"/api/v1/replays/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ComparisonNotFound_ShouldReturn404()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        var left = Guid.NewGuid();
        var right = Guid.NewGuid();

        var response = await client.GetAsync(
            $"/api/v1/comparisons?leftRunId={left}&rightRunId={right}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
