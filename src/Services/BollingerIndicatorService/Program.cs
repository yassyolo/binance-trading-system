using BollingerIndicatorService;
using BollingerIndicatorService.Services;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

var redisConnectionString =
    builder.Configuration.GetSection("Redis")["ConnectionString"] ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddHttpClient<BinanceHistoricalKlineClient>();

builder.Services.AddSingleton<BollingerEngine>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();