using AlligatorIndicatorService;
using AlligatorIndicatorService.Configuration;
using AlligatorIndicatorService.Services;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<AlligatorOptions>( builder.Configuration.GetSection("Alligator"));

var redisConnectionString = builder.Configuration.GetSection("Redis")["ConnectionString"] ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddHttpClient<BinanceHistoricalKlineClient>();

builder.Services.AddSingleton<AlligatorMaEngine>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();