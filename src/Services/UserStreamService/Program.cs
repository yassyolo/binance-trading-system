using StackExchange.Redis;
using UserStreamService;
using UserStreamService.Clients;
using UserStreamService.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var redisConnectionString = builder.Configuration.GetSection("Redis")["ConnectionString"] ?? "localhost:6379";

var redis = ConnectionMultiplexer.Connect(redisConnectionString);

await redis.GetDatabase().PingAsync();

builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

builder.Services.AddHttpClient<BinanceListenKeyClient>();
builder.Services.AddHttpClient<BinanceFuturesOrdersSnapshotClient>();

builder.Services.AddSingleton<RedisPublisher>();
builder.Services.AddSingleton<UserStreamProcessor>();
builder.Services.AddSingleton<HealingService>();
builder.Services.AddSingleton<BinanceUserStreamClient>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();