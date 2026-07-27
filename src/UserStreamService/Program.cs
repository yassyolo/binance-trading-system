using TradingSystem.Binance.DependencyInjection;
using TradingSystem.Binance.UserStream;
using TradingSystem.Infrastructure.DependencyInjection;
using TradingSystem.Operations;
using TradingSystem.Persistence.PostgreSql.Configuration;
using TradingSystem.Redis.DependencyInjection;
using UserStreamService;
using UserStreamService.Configuration;
using UserStreamService.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTradingInfrastructure();
builder.Services.AddTradingRedis(builder.Configuration);
builder.Services.AddBinanceFutures(builder.Configuration);

builder.Services
    .AddOptions<BinanceUserStreamOptions>()
    .Bind(builder.Configuration.GetSection(BinanceUserStreamOptions.SectionName))
    .Validate(x => Uri.TryCreate(x.RestBaseUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps,
        "A valid HTTPS Binance user-stream REST base URL is required.")
    .Validate(x => Uri.TryCreate(x.WebSocketBaseUrl, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == "wss" || uri.Scheme == "ws"),
        "A valid Binance user-stream WebSocket base URL is required.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.ApiKey),
        "Binance user-stream API key is required.")
    .Validate(x => x.ListenKeyKeepAliveSeconds > 0 && x.ListenKeyKeepAliveSeconds < 3600,
        "Listen-key keepalive must be between 1 and 3599 seconds.")
    .Validate(x => x.KeepAliveIntervalSeconds > 0,
        "WebSocket keepalive interval must be positive.")
    .Validate(x => x.ReceiveBufferSizeBytes >= 4096,
        "Receive buffer size must be at least 4096 bytes.")
    .ValidateOnStart();

builder.Services
    .AddOptions<UserStreamServiceOptions>()
    .Bind(builder.Configuration.GetSection(UserStreamServiceOptions.SectionName))
    .Validate(x => x.ReconnectDelaySeconds > 0,
        "Reconnect delay must be positive.")
    .Validate(x => x.MinDowntimeForHealingSeconds >= 0,
        "Minimum downtime for healing cannot be negative.")
    .Validate(x => x.HealingCooldownSeconds >= 0,
        "Healing cooldown cannot be negative.")
    .Validate(x => x.HealingSymbols.Any(symbol => !string.IsNullOrWhiteSpace(symbol)),
        "At least one non-empty healing symbol is required.")
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
