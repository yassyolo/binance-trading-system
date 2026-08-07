using TradingSystem.Operations;
using TradingSystem.Indicators;
using IndicatorServices.Workers;
using TradingSystem.Redis;
using TradingSystem.Persistence.PostgreSql;
using TradingSystem.Infrastructure;
using TradingSystem.Binance;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTradingInfrastructure();
builder.Services.AddTradingRedis(builder.Configuration);
builder.Services.AddBinancePublicMarketData(builder.Configuration); 
builder.Services.AddTradingIndicators(builder.Configuration);
builder.Services.AddPostgresTradingHistory(builder.Configuration);
builder.Services.AddServiceHeartbeat(builder.Configuration, "IndicatorServices");

builder.Services.AddHostedService<IndicatorProcessingWorker>();

await builder.Build().RunAsync();
