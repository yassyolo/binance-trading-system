using Microsoft.Extensions.Options;
using TradingSystem.Infrastructure;
using TradingSystem.Redis;
using TradingViewWebhookService.Configuration;
using TradingViewWebhookService.Models;
using TradingViewWebhookService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddOptions<TradingViewWebhookOptions>().Bind(builder.Configuration.GetSection(TradingViewWebhookOptions.SectionName)).ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<TradingViewWebhookOptions>, TradingViewWebhookOptionsValidator>();

builder.Services.AddTradingInfrastructure();
builder.Services.AddTradingRedis(builder.Configuration, subscribeToSignals: false);

builder.Services.AddSingleton<TradingViewSignalPublisher>();

var app = builder.Build();

app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "ok",
        service = "TradingViewWebhookService",
        utc = DateTime.UtcNow
    }));

app.MapPost("/api/v1/tradingview/{botName}", async (string botName, TradingViewSignalRequest request, TradingViewSignalPublisher publisher, CancellationToken ct) =>
    {
        var result = await publisher.PublishAsync(botName, request, ct);

        if (!result.Succeeded)
            return Results.Json(new { error = result.Error }, statusCode: result.StatusCode);

        return Results.Accepted(value: result.Response);
    });

await app.RunAsync();
