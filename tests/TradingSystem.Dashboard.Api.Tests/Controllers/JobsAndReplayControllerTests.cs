using Microsoft.AspNetCore.Mvc;
using Moq;
using TradingSystem.Dashboard.Api.Controllers;
using TradingSystem.Dashboard.Api.Tests.Infrastructure;
using TradingSystem.Dashboard.Application.Contracts;
using TradingSystem.Dashboard.Contracts.Models.Backtesting;
using TradingSystem.Dashboard.Contracts.Models.Jobs;
using TradingSystem.Dashboard.Contracts.Models.Optimization;
using TradingSystem.ReplayEngine.Models;
using TradingSystem.ReplayEngine.Models.Enums;
using TradingSystem.ReplayEngine.Store;
using Xunit;

namespace TradingSystem.Dashboard.Api.Tests.Controllers;

public sealed class JobsAndReplayControllerTests
{
    [Fact]
    public async Task Backtest_ShouldReturnAccepted_AndPassActor()
    {
        var store = new Mock<IDashboardJobStore>();

        var request = ValidBacktest();

        var acceptedDto =
            new JobAcceptedDto(
                Guid.NewGuid(),
                "Backtest",
                "Pending",
                DateTime.UtcNow);

        store
            .Setup(x => x.EnqueueBacktestAsync(
                request,
                "operator-a",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(acceptedDto);

        var controller =
            new BacktestsController(store.Object)
                .WithUser(
                    role: "Operator",
                    user: "operator-a");

        var result =
            await controller.CreateAsync(
                request,
                default);

        var accepted =
            Assert.IsType<AcceptedResult>(result);

        Assert.Same(acceptedDto, accepted.Value);
    }

    [Fact]
    public async Task Optimization_ShouldReturnAccepted()
    {
        var store = new Mock<IDashboardJobStore>();

        var request = new OptimizationRequest(
            "BOT8012",
            "BTCUSDC",
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow,
            10_000m,
            "Internal",
            [new OptimizationRangeDto(
                "ProfitDistance",
                100m,
                300m,
                100m)],
            10,
            false,
            null,
            null,
            null);

        store
            .Setup(x => x.EnqueueOptimizationAsync(
                request,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new JobAcceptedDto(
                    Guid.NewGuid(),
                    "Optimization",
                    "Pending",
                    DateTime.UtcNow));

        var controller =
            new OptimizationsController(
                store.Object,
                Mock.Of<
                    TradingSystem.Dashboard.Application.Contracts.IDashboardQueryStore>())
                .WithUser(role: "Operator");

        var result =
            await controller.CreateAsync(
                request,
                default);

        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task ReplayCreate_ShouldClampBatchSizeTo1000()
    {
        var store = new Mock<IReplayJobStore>();
        CreateReplayRequest? captured = null;

        var replayId = Guid.NewGuid();

        store
            .Setup(x => x.EnqueueAsync(
                It.IsAny<CreateReplayRequest>(),
                "operator-a",
                It.IsAny<CancellationToken>()))
            .Callback<CreateReplayRequest, string, CancellationToken>(
                (request, _, _) => captured = request)
            .ReturnsAsync(replayId);

        var controller =
            new ReplaysController(store.Object)
                .WithUser(
                    role: "Operator",
                    user: "operator-a");

        var result =
            await controller.CreateAsync(
                new CreateReplayRequest(
                    "Replay 1",
                    ReplayMode.Timeline,
                    BatchSize: 50_000),
                default);

        var accepted =
            Assert.IsType<AcceptedResult>(result);

        Assert.NotNull(captured);
        Assert.Equal(1000, captured!.BatchSize);
        Assert.Equal(
            $"/api/v1/replays/{replayId}",
            accepted.Location);
    }

    [Fact]
    public async Task ReplayGet_WhenMissing_ShouldReturnNotFound()
    {
        var store = new Mock<IReplayJobStore>();

        store
            .Setup(x => x.GetAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReplayJob?)null);

        var controller =
            new ReplaysController(store.Object)
                .WithUser(role: "Viewer");

        var result =
            await controller.GetAsync(
                Guid.NewGuid(),
                default);

        Assert.IsType<NotFoundResult>(result);
    }

    private static BacktestRequest ValidBacktest() =>
    new(
        "BOT8012",
        "BTCUSDC",
        DateTime.UtcNow.AddDays(-1),
        DateTime.UtcNow,
        10_000m,
        "Internal",
        0.04m,
        0.01m,
        new Dictionary<string, string>());
}
