using Moq;
using System.Net;
using TradingSystem.Dashboard.Api.Tests.Infrastructure;
using TradingSystem.Dashboard.Application.Models;
using Xunit;

namespace TradingSystem.Dashboard.Api.Tests.Integration;

public sealed class FailureMappingTests
{
    [Fact]
    public async Task UnexpectedStoreException_ShouldReturn500_WithoutLeakingMessage()
    {
        await using var factory = new DashboardApiFactory();

        factory.QueryStore
            .Setup(x => x.GetSignalsAsync(
                It.IsAny<DashboardQuery>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new Exception(
                    "SECRET DATABASE INTERNAL DETAIL"));

        using var client = factory.CreateClient("Viewer");

        var response = await client.GetAsync(
            "/api/v1/signals?skip=0&take=10");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "Unexpected server error",
            body);

        Assert.DoesNotContain(
            "SECRET DATABASE INTERNAL DETAIL",
            body);
    }

    [Fact]
    public async Task UnauthorizedAccessException_ShouldMapTo403()
    {
        await using var factory = new DashboardApiFactory();

        factory.QueryStore
            .Setup(x => x.GetSignalsAsync(
                It.IsAny<DashboardQuery>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new UnauthorizedAccessException(
                    "internal permission detail"));

        using var client = factory.CreateClient("Viewer");

        var response = await client.GetAsync(
            "/api/v1/signals?skip=0&take=10");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(
            "internal permission detail",
            body);
    }
}
