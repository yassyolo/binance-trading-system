using Microsoft.AspNetCore.Mvc;
using Moq;
using TradingSystem.Dashboard.Api.Controllers;
using TradingSystem.Dashboard.Api.Exceptions;
using TradingSystem.Dashboard.Api.Tests.Infrastructure;
using TradingSystem.Dashboard.Application.Contracts;
using TradingSystem.Dashboard.Contracts.Models.Bots;
using TradingSystem.Dashboard.Contracts.Models.Enums;
using Xunit;

namespace TradingSystem.Dashboard.Api.Tests.Controllers;

public sealed class BotsControllerTests
{
    [Fact]
    public async Task GetAll_ShouldReturnOk_AndDelegateToStore()
    {
        var configurations =
            new Mock<IBotConfigurationStore>();

        configurations
            .Setup(x => x.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var controller = new BotsController(
            configurations.Object,
            Mock.Of<IBotCommandStore>())
            .WithUser();

        var result = await controller.GetAllAsync(default);

        Assert.IsType<OkObjectResult>(result);

        configurations.Verify(
            x => x.GetAllAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Update_ShouldPassAuthenticatedUserToStore()
    {
        var store =
            new Mock<IBotConfigurationStore>();

        var request = ValidConfiguration();

        var updated = new BotConfigurationDto(
            "BOT8012",
            "Grid",
            "BTCUSDC",
            TradingEnvironment.Demo,
            "Internal",
            true,
            true,
            0.001m,
            10,
            100m,
            50m,
            2,
            60,
            2,
            DateTime.UtcNow,
            "alice",
            false);

        store
            .Setup(x => x.UpdateAsync(
                "BOT8012",
                request,
                "alice",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var controller = new BotsController(
            store.Object,
            Mock.Of<IBotCommandStore>())
            .WithUser(user: "alice");

        var result =
            await controller.UpdateConfigurationAsync(
                "BOT8012",
                request,
                default);

        Assert.IsType<OkObjectResult>(result);

        store.Verify(
            x => x.UpdateAsync(
                "BOT8012",
                request,
                "alice",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenStoreReportsConflict_ShouldThrowApiConflictException()
    {
        var store =
            new Mock<IBotConfigurationStore>();

        store
            .Setup(x => x.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<UpdateBotConfigurationRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Version conflict."));

        var controller = new BotsController(
            store.Object,
            Mock.Of<IBotCommandStore>())
            .WithUser();

        await Assert.ThrowsAsync<ApiConflictException>(
            () => controller.UpdateConfigurationAsync(
                "BOT8012",
                ValidConfiguration(),
                default));
    }

    [Fact]
    public async Task Command_ShouldReturnAccepted_AndPassActor()
    {
        var commands =
            new Mock<IBotCommandStore>();

        var request = new BotCommandRequest(
            BotCommandType.Start,
            "Start it",
            true);

        var dto = new BotCommandDto(
            Guid.NewGuid(),
            "BOT8012",
            BotCommandType.Start,
            BotCommandStatus.Pending,
            "operator-a",
            "Start it",
            DateTime.UtcNow,
            null,
            null);

        commands
            .Setup(x => x.EnqueueAsync(
                "BOT8012",
                request,
                "operator-a",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = new BotsController(
            Mock.Of<IBotConfigurationStore>(),
            commands.Object)
            .WithUser(
                role: "Operator",
                user: "operator-a");

        var result =
            await controller.EnqueueCommandAsync(
                "BOT8012",
                request,
                default);

        var accepted =
            Assert.IsType<AcceptedResult>(result);

        Assert.Same(dto, accepted.Value);

        commands.Verify(
            x => x.EnqueueAsync(
                "BOT8012",
                request,
                "operator-a",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static UpdateBotConfigurationRequest
        ValidConfiguration() =>
        new(
            1,
            "Grid",
            "BTCUSDC",
            TradingEnvironment.Demo,
            "Internal",
            true,
            true,
            0.001m,
            10,
            100m,
            50m,
            2,
            60,
            "Test");
}
