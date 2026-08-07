using TradingSystem.Operations;
using TradingSystem.Backtesting.Bots;
using TradingSystem.Binance.Market;
using TradingSystem.Jobs.Worker.Execution;
using TradingSystem.Jobs.Worker.Workers;
using TradingSystem.Optimization;
using TradingSystem.Persistence.PostgreSql.HistoricalData;
using TradingSystem.Persistence.PostgreSql.Jobs;
using TradingSystem.Persistence.PostgreSql;
using TradingSystem.Jobs.Worker.Configuration;
using TradingSystem.JobOrchestration.Contracts;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<JobWorkerOptions>(builder.Configuration.GetSection(JobWorkerOptions.SectionName));
builder.Services.Configure<HistoricalDataIngestionOptions>(builder.Configuration.GetSection(HistoricalDataIngestionOptions.SectionName));
builder.Services.AddPostgresTradingHistory(builder.Configuration);
builder.Services.AddServiceHeartbeat(builder.Configuration, "TradingJobsWorker");
builder.Services.AddAlertEngine(builder.Configuration);
builder.Services.AddBotBacktesting();builder.Services.AddTradingOptimization();
builder.Services.AddSingleton<IDashboardJobQueue, PostgresDashboardJobQueue>();
builder.Services.AddSingleton<PostgresHistoricalMarketDataStore>();
builder.Services.AddSingleton<IHistoricalMarketDataStore>(s => s.GetRequiredService<PostgresHistoricalMarketDataStore>());
builder.Services.AddSingleton<IHistoricalSignalStore>(s => s.GetRequiredService<PostgresHistoricalMarketDataStore>());

var ingestion = builder.Configuration.GetSection(HistoricalDataIngestionOptions.SectionName).Get<HistoricalDataIngestionOptions>()??new();

var baseAddress = ingestion.Environment.Equals("Demo", StringComparison.OrdinalIgnoreCase)?"https://demo-fapi.binance.com":"https://fapi.binance.com";

builder.Services.AddHttpClient<BinanceHistoricalCandleRangeSource>(c => {c.BaseAddress = new Uri(baseAddress);c.Timeout = TimeSpan.FromSeconds(60);});
builder.Services.AddSingleton<TradingSystem.Application.MarketData.IHistoricalCandleRangeSource>(s => s.GetRequiredService<BinanceHistoricalCandleRangeSource>());
builder.Services.AddSingleton<BacktestExecutionService>();
builder.Services.AddSingleton<OptimizationExecutionService>();builder.Services.AddHostedService<BacktestingJobWorker>();
builder.Services.AddHostedService<OptimizationJobWorker>();builder.Services.AddHostedService<HistoricalDataIngestionWorker>();
builder.Services.AddHostedService<ReplayJobWorker>();
await builder.Build().RunAsync();
