using StackExchange.Redis;
using StrategyService;
using StrategyService.Configuration;
using StrategyService.Execution;
using StrategyService.Positions;
using StrategyService.Services;
using StrategyService.Strategies;
using StrategyService.Strategies.Bot8011;
using StrategyService.Strategies.Bot8012;
using TradingSystem.Application.Engine;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Market;
using TradingSystem.Binance.Orders;
using TradingSystem.Redis.Configuration;
using TradingSystem.Redis.Positions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<TradingEngine>();

builder.Services.AddSingleton<IMarketPriceProvider, BinanceMarketPriceProvider>();
builder.Services.AddSingleton<IActivePositionProvider, RedisActivePositionProvider>();

builder.Services.AddSingleton<ITradeExecutor, CompositeTradeExecutor>();

builder.Services.AddSingleton<ITradingStrategy, Bot8012Strategy>();
builder.Services.AddSingleton<IBotTradeExecutor, Bot8012TradeExecutor>();

builder.Services.AddSingleton<Bot8012PositionEventService>();
builder.Services.AddSingleton<Bot8012GapPolicy>();

builder.Services.AddSingleton<SignalProcessor>();
builder.Services.AddHostedService<RedisSignalSubscriber>();



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
builder.Services.AddSingleton<Bot8011PositionEventService>();
builder.Services.AddSingleton<PositionManager>();
builder.Services.AddSingleton<OrderExecutionService>();

builder.Services.AddSingleton<TradingEngine>();

builder.Services.AddSingleton<UserStreamOrderSubscriber>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<UserStreamOrderSubscriber>());

builder.Services.AddSingleton<Bot8012PositionEventService>();

builder.Services.AddSingleton<RedisActivePositionProvider>();

builder.Services.AddSingleton<IBotActivePositionProvider, Bot8011ActivePositionProvider>();
builder.Services.AddSingleton<IBotActivePositionProvider, Bot8012ActivePositionProvider>();

builder.Services.AddSingleton<IActivePositionProvider, CompositeActivePositionProvider>();

builder.Services.AddSingleton<ITradingStrategy, Bot8012Strategy>();
builder.Services.AddSingleton<Bot8012GapPolicy>();

builder.Services.AddSingleton<IBotTradeExecutor, Bot8012TradeExecutor>();

builder.Services.AddSingleton<IMarketPriceProvider, BinanceMarketPriceProvider>();
builder.Services.AddSingleton<IActivePositionProvider, RedisActivePositionProvider>();

builder.Services.AddSingleton<ITradingStrategy, Bot8012Strategy>();
builder.Services.AddSingleton<Bot8012GapPolicy>();
builder.Services.AddSingleton<ITradingStrategy, Bot8012Strategy>();
builder.Services.AddSingleton<ITradeExecutor, CompositeTradeExecutor>();

builder.Services.AddSingleton<IBotTradeExecutor, Bot8012TradeExecutor>();

builder.Services.AddSingleton<ITradingStrategy, Bot8012Strategy>();
builder.Services.AddSingleton<Bot8012GapPolicy>();
builder.Services.AddSingleton<IBotStrategy, Bot8011Strategy>();
builder.Services.AddSingleton<IBotStrategy, Bot8013Strategy>();
builder.Services.AddSingleton<IBotStrategy, Bot8014Strategy>();
builder.Services.AddSingleton<IBotStrategy, Bot8015Strategy>();
builder.Services.AddSingleton<IBotStrategy, Bot8016Strategy>();

builder.Services.AddSingleton<SignalProcessor>();

builder.Services.AddHttpClient<BinanceExchangeInfoService>();

builder.Services.AddSingleton<PositionLockService>();
builder.Services.AddSingleton<SafeBinanceOrderService>();
builder.Services.AddSingleton<Bot8011EffectivePositionService>();

builder.Services.AddHostedService<BinanceStartupService>();

builder.Services.AddSingleton<OrderEventDeduplicationService>();

builder.Services.AddSingleton<Bot8011RedisCleanupService>();
builder.Services.AddHostedService<Bot8011StartupCleanupHostedService>();

builder.Services.AddSingleton<Bot8011HealingService>();
builder.Services.AddHostedService<HealingSnapshotSubscriber>();

builder.Services.AddHttpClient<IBinanceFuturesMarketClient, BinanceFuturesMarketClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["BinanceFutures:BaseUrl"]
        ?? "https://fapi.binance.com");
});
builder.Services.Configure<TelegramOptions>(
    builder.Configuration.GetSection("Telegram"));

builder.Services.AddSingleton<BinanceRetryService>();
builder.Services.AddSingleton<Bot8011Stop3OrderService>();

builder.Services.AddSingleton<Bot8011ManualPositionRecoveryService>();
builder.Services.AddHostedService<Bot8011ManualRecoveryHostedService>();

builder.Services.AddHttpClient<TelegramNotificationService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<UserStreamOrderSubscriber>();
builder.Services.AddHostedService<Bot8011Stop3TrailingWorker>();

var host = builder.Build();
host.Run();