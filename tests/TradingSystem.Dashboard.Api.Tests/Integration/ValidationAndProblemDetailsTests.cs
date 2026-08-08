using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TradingSystem.Dashboard.Api.Tests.Infrastructure;
using TradingSystem.Dashboard.Contracts.Models.Bots;
using TradingSystem.Dashboard.Contracts.Models.Enums;
using Xunit;

namespace TradingSystem.Dashboard.Api.Tests.Integration;

public sealed class ValidationAndProblemDetailsTests
{
    [Fact]
    public async Task NegativeSkip_ShouldReturn400ProblemDetails()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        var response = await client.GetAsync(
            "/api/v1/signals?skip=-1&take=100");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation failed", body);
        Assert.Contains("errors", body);
        Assert.Contains("traceId", body);
    }

    [Fact]
    public async Task UnsupportedChartInterval_ShouldReturn400()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        var from = Uri.EscapeDataString(
            DateTime.UtcNow.AddHours(-1).ToString("O"));
        var to = Uri.EscapeDataString(
            DateTime.UtcNow.ToString("O"));

        var response = await client.GetAsync(
            $"/api/v1/charts/BTCUSDC/7m?fromUtc={from}&toUtc={to}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidBotName_ShouldReturn400()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Viewer");

        var response = await client.GetAsync(
            "/api/v1/bots/BOT%208012");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidAlertId_ShouldReturn400()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Operator");

        var response = await client.PostAsync(
            "/api/v1/alerts/0/acknowledge",
            null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MalformedJson_ShouldReturn400()
    {
        await using var factory = new DashboardApiFactory();
        using var client = factory.CreateClient("Operator");

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            "/api/v1/bots/BOT8012/configuration");

        request.Content = new StringContent(
            "{ this-is-not-json",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConflictFromConfigurationStore_ShouldReturn409()
    {
        await using var factory = new DashboardApiFactory();

        factory.BotConfigurations
            .Setup(x => x.UpdateAsync(
                "BOT8012",
                It.IsAny<UpdateBotConfigurationRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Configuration version conflict."));

        using var client = factory.CreateClient(
            "Operator",
            "alice");

        var request = ValidConfiguration();

        var response = await client.PutAsJsonAsync(
            "/api/v1/bots/BOT8012/configuration",
            request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Conflict", body);
        Assert.Contains("Configuration version conflict.", body);
    }

    private static UpdateBotConfigurationRequest
        ValidConfiguration() =>
        new(
            ExpectedVersion: 1,
            StrategyType: "Grid",
            Symbol: "BTCUSDC",
            Environment: TradingEnvironment.Demo,
            SignalSource: "Internal",
            EnableLong: true,
            EnableShort: true,
            Quantity: 0.001m,
            Leverage: 10,
            PriceDistance: 100m,
            ProfitDistance: 50m,
            OrderSideLimit: 2,
            CooldownSeconds: 60,
            Reason: "Integration test");
}
