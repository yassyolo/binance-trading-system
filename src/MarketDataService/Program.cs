using MarketDataService.Configuration;
using MarketDataService.Services;
using MarketDataService.Workers;
using TradingSystem.Infrastructure;
using TradingSystem.Operations;
using TradingSystem.Persistence.PostgreSql;
using TradingSystem.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddOptions<MarketDataOptions>()
    .Bind(builder.Configuration.GetSection(MarketDataOptions.SectionName))
    .Validate(MarketDataOptions.IsValid, MarketDataOptions.ValidationError)
    .ValidateOnStart();

builder.Services.AddTradingInfrastructure();
builder.Services.AddTradingRedis(builder.Configuration);
builder.Services.AddSingleton<KlinePublisher>();
builder.Services.AddHostedService<MarketDataWorker>();
builder.Services.AddPostgresTradingHistory(builder.Configuration);
builder.Services.AddServiceHeartbeat(builder.Configuration, "MarketDataService");

await builder.Build().RunAsync();
