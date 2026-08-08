using System.Net;
using System.Net.Http.Json;
using TradingSystem.Dashboard.Api.Tests.Infrastructure;
using TradingSystem.Dashboard.Contracts.Models.Bots;
using TradingSystem.Dashboard.Contracts.Models.Enums;
using Xunit;

namespace TradingSystem.Dashboard.Api.Tests.Integration;

public sealed class IdempotencyBoundaryTests
{
    [Fact]
    public async Task BotCommand_WithoutIdempotencyKey_ShouldReturn400()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Operator");

        var request = new BotCommandRequest(
            BotCommandType.Start,
            "Start from dashboard",
            true);

        var response = await client.PostAsJsonAsync(
            "/api/v1/bots/BOT8012/commands",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("X-Idempotency-Key", body);
    }

    [Fact]
    public async Task BotCommand_WithTooShortIdempotencyKey_ShouldReturn400()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Operator");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/bots/BOT8012/commands");

        request.Headers.Add(
            "X-Idempotency-Key",
            "short");

        request.Content = JsonContent.Create(
            new BotCommandRequest(
                BotCommandType.Start,
                "Start from dashboard",
                true));

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ViewerForbidden_ShouldHappenBeforeIdempotencyValidation()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        var response = await client.PostAsJsonAsync(
            "/api/v1/bots/BOT8012/commands",
            new BotCommandRequest(
                BotCommandType.Start,
                "Attempt",
                true));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}
