using global::Prometheus;
using System.Diagnostics.Metrics;

namespace TradingSystem.Prometheus;

public sealed class TradingMetrics
{
    public Counter SignalsReceived { get; } = Metrics.CreateCounter(
        "trading_signals_received_total",
        "Trading signals received by the strategy service.",
        new CounterConfiguration { LabelNames = ["bot", "symbol", "side", "source"] });

    public Counter Decisions { get; } = Metrics.CreateCounter(
        "trading_strategy_decisions_total",
        "Strategy decisions.",
        new CounterConfiguration { LabelNames = ["bot", "symbol", "side", "decision"] });

    public Counter Executions { get; } = Metrics.CreateCounter(
        "trading_executions_total",
        "Trading execution outcomes.",
        new CounterConfiguration { LabelNames = ["bot", "symbol", "side", "result"] });

    public Counter OrderEvents { get; } = Metrics.CreateCounter(
        "trading_order_events_total",
        "Binance user-stream order events.",
        new CounterConfiguration { LabelNames = ["bot", "symbol", "role", "status"] });

    public Counter ProcessingFailures { get; } = Metrics.CreateCounter(
        "trading_processing_failures_total",
        "Signal or order processing failures.",
        new CounterConfiguration { LabelNames = ["component", "bot", "exception"] });

    public Counter RiskDecisions { get; } = Metrics.CreateCounter(
        "trading_risk_decisions_total",
        "Central risk admission decisions.",
        new CounterConfiguration { LabelNames = ["bot", "symbol", "side", "decision", "code"] });

    public Histogram SignalProcessingDuration { get; } = Metrics.CreateHistogram(
        "trading_signal_processing_duration_seconds",
        "End-to-end signal processing duration.",
        new HistogramConfiguration
        {
            LabelNames = ["bot", "symbol"],
            Buckets = Histogram.ExponentialBuckets(0.005, 2, 14)
        });

    public Histogram BinanceRequestDuration { get; } = Metrics.CreateHistogram(
        "trading_binance_request_duration_seconds",
        "Binance request duration.",
        new HistogramConfiguration
        {
            LabelNames = ["operation", "result"],
            Buckets = Histogram.ExponentialBuckets(0.01, 2, 14)
        });

    public Gauge PortfolioEquity { get; } = Metrics.CreateGauge(
        "trading_portfolio_equity",
        "Current estimated portfolio equity.");

    public Gauge PortfolioUnrealizedPnl { get; } = Metrics.CreateGauge(
        "trading_portfolio_unrealized_pnl",
        "Current portfolio unrealized PnL.");

    public Gauge PortfolioDailyRealizedPnl { get; } = Metrics.CreateGauge(
        "trading_portfolio_daily_realized_pnl",
        "Current UTC-day realized PnL.");

    public Gauge PortfolioDrawdownPercent { get; } = Metrics.CreateGauge(
        "trading_portfolio_daily_drawdown_percent",
        "Current UTC-day drawdown percentage.");

    public Gauge OpenPositions { get; } = Metrics.CreateGauge(
        "trading_open_positions",
        "Open positions.",
        new GaugeConfiguration { LabelNames = ["bot", "symbol", "side"] });

    public Gauge GrossNotional { get; } = Metrics.CreateGauge(
        "trading_gross_notional",
        "Gross portfolio notional.");

    public Gauge NetNotionalBySymbol { get; } = Metrics.CreateGauge(
        "trading_symbol_net_notional",
        "Net notional by symbol.",
        new GaugeConfiguration { LabelNames = ["symbol"] });

    public Gauge ReconciliationCriticalFindings { get; } = Metrics.CreateGauge(
        "trading_reconciliation_critical_findings",
        "Current unresolved critical reconciliation findings.");
}
