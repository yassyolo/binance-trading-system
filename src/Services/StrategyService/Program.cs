using StrategyService;
using StrategyService.Configuration;
using StrategyService.Services;
using StrategyService.Strategies;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<Bot8011Options>(
    builder.Configuration.GetSection("Bot8011"));

builder.Services.AddSingleton<PositionManager>();
builder.Services.AddSingleton<OrderExecutionService>();
builder.Services.AddSingleton<Bot8011Strategy>();
builder.Services.AddSingleton<SignalProcessor>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();