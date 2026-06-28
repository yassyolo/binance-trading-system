using StackExchange.Redis;
using StrategyService;
using StrategyService.Configuration;
using StrategyService.Services;
using StrategyService.Strategies;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Orders;
using TradingSystem.Redis.Configuration;
using TradingSystem.Redis.Positions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<Bot8011Options>(
    builder.Configuration.GetSection("Bot8011"));
builder.Services.Configure<Bot8012Options>(
    builder.Configuration.GetSection("Bot8012"));
builder.Services.Configure<Bot8013Options>(
    builder.Configuration.GetSection("Bot8013"));
builder.Services.Configure<Bot8014Options>(
    builder.Configuration.GetSection("Bot8014"));
builder.Services.Configure<Bot8015Options>(
    builder.Configuration.GetSection("Bot8015"));
builder.Services.Configure<Bot8016Options>(
    builder.Configuration.GetSection("Bot8016"));

builder.Services.Configure<RedisPositionStoreOptions>(
    builder.Configuration.GetSection("RedisPositionStore"));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379"));

builder.Services.AddSingleton<IPositionStore, RedisPositionStore>();

builder.Services.Configure<BinanceFuturesOptions>(
    builder.Configuration.GetSection("BinanceFutures"));

builder.Services.AddHttpClient<IBinanceFuturesOrderClient, BinanceFuturesOrderClient>();

builder.Services.AddSingleton<PositionManager>();
builder.Services.AddSingleton<OrderExecutionService>();
builder.Services.AddSingleton<SignalProcessor>();

builder.Services.AddSingleton<Bot8011Strategy>();
builder.Services.AddSingleton<Bot8012Strategy>();
builder.Services.AddSingleton<Bot8013Strategy>();
builder.Services.AddSingleton<Bot8014Strategy>();
builder.Services.AddSingleton<Bot8015Strategy>();
builder.Services.AddSingleton<Bot8016Strategy>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();