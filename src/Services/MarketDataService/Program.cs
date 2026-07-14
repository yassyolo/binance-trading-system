using MarketDataService;
using MarketDataService.Configuration;
using MarketDataService.Services;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<MarketDataOptions>()
    .Bind(builder.Configuration.GetSection(MarketDataOptions.SectionName))
    .Validate(options => options.Symbols.Length > 0, "At least one symbol is required.")
    .Validate(options => options.Intervals.Length > 0, "At least one interval is required.")
    .ValidateOnStart();

var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connection = ConnectionMultiplexer.Connect(redisConnectionString);
    connection.GetDatabase().Ping();
    return connection;
});

builder.Services.AddSingleton<KlinePublisher>();
builder.Services.AddHostedService<Worker>();

await builder.Build().RunAsync();
