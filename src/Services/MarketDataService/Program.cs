using MarketDataService;
using TradingSystem.Infrastructure.DependencyInjection;
using TradingSystem.Operations;
using TradingSystem.Persistence.PostgreSql.Configuration;
using TradingSystem.Redis;
using TradingSystem.Redis.DependencyInjection;
var builder = Host.CreateApplicationBuilder(args); builder.Configuration.AddEnvironmentVariables(); builder.Services.AddOptions<MarketDataOptions>().Bind(builder.Configuration.GetSection(MarketDataOptions.SectionName)).Validate(x => x.Symbols.Length > 0 && x.Intervals.Length > 0, "Symbols and intervals are required").ValidateOnStart(); builder.Services.AddTradingInfrastructure(); builder.Services.AddTradingRedis(builder.Configuration); builder.Services.AddSingleton<KlinePublisher>(); builder.Services.AddHostedService<Worker>(); builder.Services.AddPostgresTradingHistory(builder.Configuration); builder.Services.AddServiceHeartbeat(builder.Configuration, "MarketDataService"); await builder.Build().RunAsync();
