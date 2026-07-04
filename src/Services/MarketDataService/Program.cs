using MarketDataService;
using MarketDataService.Configuration;
using MarketDataService.Services;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MarketDataOptions>(builder.Configuration.GetSection("MarketData"));

var redisConnectionString = builder.Configuration.GetSection("Redis")["ConnectionString"] ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddSingleton<KlinePublisher>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();