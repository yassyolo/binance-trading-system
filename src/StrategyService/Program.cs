using StrategyService.Bots.Bot8011;
using StrategyService.Bots.Bot8012;
using StrategyService.Bots.Bot8013;
using StrategyService.Bots.Bot8014;
using StrategyService.Bots.Bot8015;
using StrategyService.Bots.Bot8016;
using StrategyService.Reliability;
using StrategyService.Runtime;
using StrategyService.Subscribers;
using TradingSystem.Application.DependencyInjection;
using TradingSystem.Application.Execution;
using TradingSystem.Binance.DependencyInjection;
using TradingSystem.HistoricalDatabase;
using TradingSystem.Infrastructure.DependencyInjection;
using TradingSystem.Observability;
using TradingSystem.Operations;
using TradingSystem.PaperTrading.Configuration;
using TradingSystem.Persistence.PostgreSql.Configuration;
using TradingSystem.Prometheus;
using TradingSystem.Reconciliation;
using TradingSystem.Redis.DependencyInjection;
using TradingSystem.RiskManagement;
using TradingSystem.Signals.Configuration;
using TradingSystem.Strategies.Alligator;
using TradingSystem.Strategies.Grid;
using TradingSystem.Strategies.Positions;
using TradingSystem.Strategies.Protection;
using TradingSystem.StrategyPlugins;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTradingInfrastructure();
builder.Services.AddTradingObservability();
builder.Services.AddTradingApplication(builder.Configuration);
builder.Services.AddSingleton<ITradingSignalHandler,TradingSignalHandler>();
builder.Services.AddStrategyPluginSystem(builder.Configuration);
builder.Services.AddPaperTrading(builder.Configuration);
builder.Services.AddTradingRedis(builder.Configuration, subscribeToSignals: true);
builder.Services.AddBinanceFutures(builder.Configuration);
builder.Services.AddTradingSignals(builder.Configuration);
builder.Services.AddPostgresTradingHistory(builder.Configuration);
builder.Services.AddServiceHeartbeat(builder.Configuration, "StrategyService");
builder.Services.AddBotRuntimeOrchestration(builder.Configuration);
builder.Services.AddCentralRiskManagement(builder.Configuration);
builder.Services.AddTradingReconciliation(builder.Configuration);

builder.Services.AddSingleton<IExchangeStateProvider, BinanceExchangeStateProvider>();
builder.Services.AddSingleton<IHealingActionExecutor, SafeHealingActionExecutor>();
builder.Services.AddHostedService<ReconciliationWorker>();

builder.Services.AddHttpClient<TelegramNotificationService>();
builder.Services.AddSingleton<TelegramTradingEngineNotifier>();

builder.Services.AddSingleton<GridSpacingPolicy>();
builder.Services.AddSingleton<PositionAdmissionPolicy>();
builder.Services.AddSingleton<Stop3Policy>();
builder.Services.AddSingleton<AlligatorEntryPolicy>();

builder.Services.AddBot8011(builder.Configuration);
builder.Services.AddBot8012(builder.Configuration);
builder.Services.AddBot8013(builder.Configuration);
builder.Services.AddBot8014(builder.Configuration);
builder.Services.AddBot8015(builder.Configuration);
builder.Services.AddBot8016(builder.Configuration);

builder.Services.AddHostedService<UserStreamOrderSubscriber>();
builder.Services.AddHostedService<HealingSnapshotSubscriber>();
builder.Services.AddHostedService<PaperPositionCloseWorker>();

builder.Services.AddHistoricalDatabase(builder.Configuration);
builder.Services.AddTradingPrometheus(builder.Configuration);

await builder.Build().RunAsync();
