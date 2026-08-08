using System.Net;
using TradingSystem.Dashboard.Api.Tests.Infrastructure;
using Xunit;

namespace TradingSystem.Dashboard.Api.Tests.Integration;

public sealed class AuthenticationAuthorizationTests
{
    [Theory]
    [InlineData("/api/v1/bots")]
    [InlineData("/api/v1/signals")]
    [InlineData("/api/v1/positions")]
    [InlineData("/api/v1/trades")]
    [InlineData("/api/v1/alerts")]
    [InlineData("/api/v1/audit")]
    public async Task ProtectedGet_WithoutAuthentication_ShouldReturn401(
        string path)
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Viewer_ShouldAccessViewerEndpoint()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        var response = await client.GetAsync("/api/v1/bots");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_ShouldNotAccessOperatorEndpoint()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        var response = await client.GetAsync("/api/v1/audit");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Operator_ShouldAccessOperatorEndpoint()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Operator");

        var response = await client.GetAsync("/api/v1/audit");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_ShouldNotAccessAdministratorReset()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        var response = await client.PostAsync(
            "/api/v1/paper/reset",
            null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Operator_ShouldNotAccessAdministratorReset()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Operator");

        var response = await client.PostAsync(
            "/api/v1/paper/reset",
            null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUnknownRoute_ShouldReturn404()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        var response = await client.GetAsync(
            "/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
