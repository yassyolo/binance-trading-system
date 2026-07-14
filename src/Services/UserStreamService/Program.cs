using StackExchange.Redis;
using UserStreamService;
using UserStreamService.Clients;
using UserStreamService.Configuration;
using UserStreamService.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services
    .AddOptions<BinanceUserStreamOptions>()
    .Bind(builder.Configuration.GetSection(BinanceUserStreamOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Binance API key is required.")
    .ValidateOnStart();

builder.Services
    .AddOptions<UserStreamOptions>()
    .Bind(builder.Configuration.GetSection(UserStreamOptions.SectionName))
    .Validate(options => options.ListenKeyKeepAliveSeconds > 0, "Listen key keepalive interval must be positive.")
    .ValidateOnStart();

var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connection = ConnectionMultiplexer.Connect(redisConnectionString);
    connection.GetDatabase().Ping();
    return connection;
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient<BinanceListenKeyClient>();
builder.Services.AddHttpClient<BinanceOrdersSnapshotClient>();
builder.Services.AddSingleton<RedisPublisher>();
builder.Services.AddSingleton<UserStreamProcessor>();
builder.Services.AddSingleton<HealingService>();
builder.Services.AddSingleton<BinanceUserStreamClient>();
builder.Services.AddHostedService<Worker>();

await builder.Build().RunAsync();
