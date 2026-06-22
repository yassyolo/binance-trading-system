using StackExchange.Redis;
using UserStreamService;
using UserStreamService.Clients;
using UserStreamService.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var redisConnectionString =
    builder.Configuration.GetSection("Redis")["ConnectionString"] ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddHttpClient<BinanceListenKeyClient>();

builder.Services.AddSingleton<RedisPublisher>();

builder.Services.AddSingleton<UserStreamProcessor>();

builder.Services.AddSingleton<BinanceUserStreamClient>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();