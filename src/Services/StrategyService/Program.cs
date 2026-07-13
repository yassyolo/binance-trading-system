using StackExchange.Redis;
using StrategyService;
using StrategyService.Configuration;
using StrategyService.Infrastructure.Events;
using StrategyService.Infrastructure.Locking;
using StrategyService.Services;
using StrategyService.Signals;
using TradingSystem.Application.Engine;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Market;
using TradingSystem.Binance.Market.Contracts;
using TradingSystem.Binance.Orders;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Resilience;
using TradingSystem.Binance.Startup;
using TradingSystem.Redis.Configuration;
using TradingSystem.Redis.Positions;
using TradingSystem.Redis.Subscribers;

var builder = Host.CreateApplicationBuilder(args);

// =====================================================
// Options
// =====================================================

builder.Services
    .AddOptions<TelegramOptions>()
    .Bind(builder.Configuration.GetSection(TelegramOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<BinanceFuturesOptions>()
    .Bind(builder.Configuration.GetSection(BinanceFuturesOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<RedisPositionStoreOptions>()
    .Bind(builder.Configuration.GetSection(RedisPositionStoreOptions.SectionName))
    .ValidateOnStart();

// =====================================================
// Redis
// =====================================================

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:Redis is missing.");

    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddSingleton<IPositionStore, RedisPositionStore>();

// =====================================================
// Binance clients
// =====================================================

builder.Services.AddHttpClient<
    IBinanceFuturesOrderClient,
    BinanceFuturesOrderClient>();

builder.Services.AddHttpClient<
    IBinanceFuturesMarketClient,
    BinanceFuturesMarketClient>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["BinanceFutures:BaseUrl"]
            ?? "https://fapi.binance.com");
    });

builder.Services.AddHttpClient<BinanceExchangeInfoService>();
builder.Services.AddHttpClient<TelegramNotificationService>();

// =====================================================
// Shared services
// =====================================================

builder.Services.AddSingleton<SignalProcessor>();

builder.Services.AddSingleton<
    IMarketPriceProvider,
    BinanceMarketPriceProvider>();

builder.Services.AddSingleton<OrderExecutionService>();
builder.Services.AddSingleton<BinanceRetryService>();
builder.Services.AddSingleton<SafeBinanceOrderService>();
builder.Services.AddSingleton<PositionLockService>();
builder.Services.AddSingleton<OrderEventDeduplicationService>();

// =====================================================
// Shared trading engine
// =====================================================

builder.Services.AddTradingEngine(builder.Configuration);

// =====================================================
// Bots
// =====================================================

builder.Services.AddBot8011(builder.Configuration);
builder.Services.AddBot8012(builder.Configuration);
builder.Services.AddBot8013(builder.Configuration);
builder.Services.AddBot8014(builder.Configuration);
builder.Services.AddBot8015(builder.Configuration);
builder.Services.AddBot8016(builder.Configuration);

// =====================================================
// Shared background subscribers
// =====================================================

builder.Services.AddHostedService<RedisSignalSubscriber>();
builder.Services.AddHostedService<UserStreamOrderSubscriber>();
builder.Services.AddHostedService<HealingSnapshotSubscriber>();
builder.Services.AddHostedService<BinanceStartupService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();