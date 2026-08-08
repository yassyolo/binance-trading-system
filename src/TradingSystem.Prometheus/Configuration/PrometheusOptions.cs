using Microsoft.Extensions.Options;

namespace TradingSystem.Prometheus.Configuration;

public sealed class PrometheusOptions
{
    public const string SectionName = "Prometheus";

    public bool Enabled { get; set; } = true;
    public int Port { get; set; } = 9464;
    public string Url { get; set; } = "/metrics";
    public int PortfolioRefreshSeconds { get; set; } = 5;
}
