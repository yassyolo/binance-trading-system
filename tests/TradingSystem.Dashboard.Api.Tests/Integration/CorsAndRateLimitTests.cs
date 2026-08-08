using System.Net;
using TradingSystem.Dashboard.Api.Tests.Infrastructure;
using Xunit;

namespace TradingSystem.Dashboard.Api.Tests.Integration;

public sealed class CorsAndRateLimitTests
{
    [Fact]
    public async Task ReactOrigin_Preflight_ShouldBeAllowed()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/v1/bots");

        request.Headers.Add(
            "Origin",
            "http://localhost:5173");

        request.Headers.Add(
            "Access-Control-Request-Method",
            "GET");

        var response = await client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.NoContent
                or HttpStatusCode.OK);

        Assert.Equal(
            "http://localhost:5173",
            response.Headers
                .GetValues("Access-Control-Allow-Origin")
                .Single());
    }

    [Fact]
    public async Task ReadPolicy_ShouldEventuallyReturn429()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        HttpResponseMessage? last = null;

        for (var i = 0; i < 305; i++)
        {
            last?.Dispose();
            last = await client.GetAsync("/api/v1/bots");

            if (last.StatusCode ==
                HttpStatusCode.TooManyRequests)
            {
                break;
            }
        }

        Assert.NotNull(last);
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            last!.StatusCode);

        Assert.True(
            last.Headers.Contains("Retry-After"));

        last.Dispose();
    }
}
