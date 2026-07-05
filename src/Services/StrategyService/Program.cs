using StackExchange.Redis;
using StrategyService;
using StrategyService.Configuration;
using StrategyService.Execution;
using StrategyService.Positions;
using StrategyService.Services;
using StrategyService.Services.Healing;
using StrategyService.Strategies.Bot8011;
using StrategyService.Strategies.Bot8012;
using TradingSystem.Application.Engine;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Market;
using TradingSystem.Binance.Market.Contracts;
using TradingSystem.Binance.Orders;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Resilience;
using TradingSystem.Binance.Startup;
using TradingSystem.Redis.Configuration;
using TradingSystem.Redis.Positions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<Bot8011Options>(
    builder.Configuration.GetSection("Bot8011"));

builder.Services.Configure<Bot8012Options>(
    builder.Configuration.GetSection("Bot8012"));

builder.Services.Configure<TelegramOptions>(
    builder.Configuration.GetSection("Telegram"));

builder.Services.Configure<BinanceFuturesOptions>(
    builder.Configuration.GetSection("BinanceFutures"));

builder.Services.Configure<RedisPositionStoreOptions>(
    builder.Configuration.GetSection("RedisPositionStore"));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379"));

builder.Services.AddSingleton<IPositionStore, RedisPositionStore>();

builder.Services.AddHttpClient<IBinanceFuturesOrderClient, BinanceFuturesOrderClient>();

builder.Services.AddHttpClient<IBinanceFuturesMarketClient, BinanceFuturesMarketClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["BinanceFutures:BaseUrl"]
        ?? "https://fapi.binance.com");
});

builder.Services.AddHttpClient<BinanceExchangeInfoService>();
builder.Services.AddHttpClient<TelegramNotificationService>();

builder.Services.AddSingleton<TradingEngine>();
builder.Services.AddSingleton<SignalProcessor>();

builder.Services.AddSingleton<IMarketPriceProvider, BinanceMarketPriceProvider>();

builder.Services.AddSingleton<IActivePositionProvider, CompositeActivePositionProvider>();
builder.Services.AddSingleton<IBotActivePositionProvider, Bot8011ActivePositionProvider>();
builder.Services.AddSingleton<IBotActivePositionProvider, Bot8012ActivePositionProvider>();

builder.Services.AddSingleton<ITradeExecutor, CompositeTradeExecutor>();
builder.Services.AddSingleton<IBotTradeExecutor, Bot8011TradeExecutor>();
builder.Services.AddSingleton<IBotTradeExecutor, Bot8012TradeExecutor>();

builder.Services.AddSingleton<ITradingStrategy, Bot8011Strategy>();
builder.Services.AddSingleton<ITradingStrategy, Bot8012Strategy>();
builder.Services.AddSingleton<Bot8012GapPolicy>();

builder.Services.AddSingleton<OrderExecutionService>();
builder.Services.AddSingleton<BinanceRetryService>();
builder.Services.AddSingleton<SafeBinanceOrderService>();
builder.Services.AddSingleton<PositionLockService>();
builder.Services.AddSingleton<OrderEventDeduplicationService>();

builder.Services.AddSingleton<Bot8011PositionEventService>();
builder.Services.AddSingleton<Bot8011Stop3OrderService>();
builder.Services.AddSingleton<Bot8011HealingService>();
builder.Services.AddSingleton<Bot8011RedisCleanupService>();
builder.Services.AddSingleton<Bot8011ManualPositionRecoveryService>();

builder.Services.AddSingleton<Bot8012PositionEventService>();
builder.Services.AddSingleton<IBotHealingService, Bot8011HealingService>();
builder.Services.AddSingleton<IBotHealingService, Bot8012HealingService>();

builder.Services.AddHostedService<HealingSnapshotSubscriber>();
builder.Services.AddHostedService<RedisSignalSubscriber>();
builder.Services.AddHostedService<UserStreamOrderSubscriber>();
builder.Services.AddHostedService<BinanceStartupService>();
builder.Services.AddHostedService<Bot8011StartupCleanupHostedService>();
builder.Services.AddHostedService<HealingSnapshotSubscriber>();
builder.Services.AddHostedService<Bot8011Stop3TrailingWorker>();
builder.Services.AddHostedService<Bot8011ManualRecoveryHostedService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();