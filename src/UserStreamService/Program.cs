using TradingSystem.Persistence.PostgreSql.Configuration;
using TradingSystem.Operations;
using TradingSystem.Binance.DependencyInjection;
using TradingSystem.Binance.UserStream;
using TradingSystem.Infrastructure.DependencyInjection;
using TradingSystem.Redis.DependencyInjection;
using UserStreamService;
using UserStreamService.Configuration;
using UserStreamService.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddTradingInfrastructure();
builder.Services.AddTradingRedis(builder.Configuration);
builder.Services.AddBinanceFutures(builder.Configuration);

builder.Services.AddOptions<BinanceUserStreamOptions>()
    .Bind(builder.Configuration.GetSection(BinanceUserStreamOptions.SectionName))
    .Validate(x => !string.IsNullOrWhiteSpace(x.ApiKey), "Binance user-stream API key is required.")
    .ValidateOnStart();
builder.Services.AddOptions<UserStreamServiceOptions>()
    .Bind(builder.Configuration.GetSection(UserStreamServiceOptions.SectionName))
    .Validate(x => x.ReconnectDelaySeconds>0, "Reconnect delay must be positive.")
    .Validate(x => x.HealingSymbols.Count>0, "At least one healing symbol is required.")
    .ValidateOnStart();

builder.Services.AddHttpClient<IBinanceListenKeyClient, BinanceListenKeyClient>();
builder.Services.AddSingleton<IBinanceUserStreamClient, BinanceUserStreamClient>();
builder.Services.AddSingleton<IBinanceOrdersSnapshotProvider, BinanceOrdersSnapshotProvider>();
builder.Services.AddSingleton<UserStreamEventProcessor>();
builder.Services.AddSingleton<HealingPublisher>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddPostgresTradingHistory(builder.Configuration);
builder.Services.AddServiceHeartbeat(builder.Configuration, "UserStreamService");

await builder.Build().RunAsync();
