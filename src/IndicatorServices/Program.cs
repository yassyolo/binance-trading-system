using TradingSystem.Persistence.PostgreSql.Configuration;
using TradingSystem.Operations;
using IndicatorServices;
using TradingSystem.Binance.DependencyInjection;
using TradingSystem.Indicators;
using TradingSystem.Infrastructure.DependencyInjection;
using TradingSystem.Redis.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddTradingInfrastructure();
builder.Services.AddTradingRedis(builder.Configuration);
builder.Services.AddBinancePublicMarketData(
    builder.Configuration); 
builder.Services.AddTradingIndicators(builder.Configuration);
builder.Services.AddHostedService<Worker>();
builder.Services.AddPostgresTradingHistory(builder.Configuration);
builder.Services.AddServiceHeartbeat(builder.Configuration, "IndicatorServices");

await builder.Build().RunAsync();
