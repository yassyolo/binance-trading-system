using System.Net;
using TradingSystem.Dashboard.Api.Tests.Infrastructure;
using Xunit;

namespace TradingSystem.Dashboard.Api.Tests.Integration;

public sealed class HealthAndHeadersTests
{
    [Fact]
    public async Task HealthLive_ShouldReturnOk_WithoutAuthentication()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthLive_ShouldIncludeSecurityHeaders()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal("nosniff",
            response.Headers.GetValues("X-Content-Type-Options").Single());

        Assert.Equal("DENY",
            response.Headers.GetValues("X-Frame-Options").Single());

        Assert.Equal("no-referrer",
            response.Headers.GetValues("Referrer-Policy").Single());

        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
        Assert.Equal("1",
            response.Headers.GetValues("X-API-Version").Single());
    }

    [Fact]
    public async Task SafeCorrelationId_ShouldBePreserved()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient();

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/health/live");

        request.Headers.Add(
            "X-Correlation-ID",
            "dashboard-test-123");

        var response = await client.SendAsync(request);

        Assert.Equal(
            "dashboard-test-123",
            response.Headers
                .GetValues("X-Correlation-ID")
                .Single());
    }

    [Fact]
    public async Task UnsafeCorrelationId_ShouldBeReplaced()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient();

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/health/live");

        request.Headers.TryAddWithoutValidation(
            "X-Correlation-ID",
            "bad correlation id !!!");

        var response = await client.SendAsync(request);

        var correlationId = response.Headers
            .GetValues("X-Correlation-ID")
            .Single();

        Assert.NotEqual(
            "bad correlation id !!!",
            correlationId);

        Assert.False(string.IsNullOrWhiteSpace(correlationId));
    }
}
