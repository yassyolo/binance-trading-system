using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using System.Threading;
using TradingSystem.Dashboard.Api.Controllers;
using TradingSystem.Dashboard.Api.Exceptions;
using TradingSystem.Dashboard.Api.Tests.Infrastructure;
using TradingSystem.Dashboard.Application.Contracts;
using TradingSystem.PaperTrading.Configuration;
using TradingSystem.PaperTrading.Contracts;
using Xunit;

namespace TradingSystem.Dashboard.Api.Tests.Controllers;

public sealed class OperationsAndPaperControllerTests
{
    [Fact]
    public async Task AlertAcknowledge_ShouldReturnNoContent_AndPassActor()
    {
        var commandStore =
            new Mock<IAlertCommandStore>();

        commandStore
            .Setup(x => x.AcknowledgeAsync(
                42,
                "operator-a",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller =
            new AlertsController(
                Mock.Of<IDashboardQueryStore>(),
                commandStore.Object)
                .WithUser(
                    role: "Operator",
                    user: "operator-a");

        var result =
            await controller.AcknowledgeAsync(
                42,
                default);

        Assert.IsType<NoContentResult>(result);

        commandStore.Verify(
            x => x.AcknowledgeAsync(
                42,
                "operator-a",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AlertAcknowledge_WithInvalidId_ShouldThrowValidationException()
    {
        var controller =
            new AlertsController(
                Mock.Of<IDashboardQueryStore>(),
                Mock.Of<IAlertCommandStore>())
                .WithUser(role: "Operator");

        await Assert.ThrowsAsync<ApiValidationException>(
            () => controller.AcknowledgeAsync(
                0,
                default));
    }

    [Fact]
    public async Task PaperReset_ShouldReturnAccepted_AndPassActor()
    {
        var store =
            new Mock<IPaperTradingStore>();

        store
            .Setup(x => x.ResetAsync(
                "admin-a",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller =
            new PaperTradingController(
                store.Object,
                Options.Create(
                    new PaperTradingOptions()))
                .WithUser(
                    role: "Administrator",
                    user: "admin-a");

        var result =
            await controller.ResetAsync(default);

        Assert.IsType<AcceptedResult>(result);

        store.Verify(
            x => x.ResetAsync(
                "admin-a",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
