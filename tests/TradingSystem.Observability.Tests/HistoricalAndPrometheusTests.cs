using TradingSystem.EventStore.Models;
using TradingSystem.HistoricalDatabase.Configuration;
using TradingSystem.Prometheus.Configuration;
using Xunit;

namespace TradingSystem.Observability.Tests;

public sealed class HistoricalAndPrometheusTests
{
    [Fact]
    public void HistoricalDatabaseOptions_DefaultOptions_Succeed()
    {
        var result = new HistoricalDatabaseOptionsValidator()
            .Validate(null, new HistoricalDatabaseOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void HistoricalDatabaseOptions_NonPositiveCommandTimeout_Fails(int value)
    {
        var result = new HistoricalDatabaseOptionsValidator().Validate(
            null,
            new HistoricalDatabaseOptions { CommandTimeoutSeconds = value });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void HistoricalDatabaseOptions_RetentionBelowThirtyDays_Fails()
    {
        var result = new HistoricalDatabaseOptionsValidator().Validate(
            null,
            new HistoricalDatabaseOptions { RetentionDays = 29 });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void HistoricalDatabaseOptions_PortfolioSnapshotBelowFiveSeconds_Fails()
    {
        var result = new HistoricalDatabaseOptionsValidator().Validate(
            null,
            new HistoricalDatabaseOptions { PortfolioSnapshotIntervalSeconds = 4 });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void PrometheusOptions_DefaultOptions_Succeed()
    {
        var result = new PrometheusOptionsValidator().Validate(null, new PrometheusOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void PrometheusOptions_InvalidPort_Fails(int port)
    {
        var result = new PrometheusOptionsValidator().Validate(
            null,
            new PrometheusOptions { Port = port });

        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("metrics")]
    public void PrometheusOptions_InvalidUrl_Fails(string url)
    {
        var result = new PrometheusOptionsValidator().Validate(
            null,
            new PrometheusOptions { Url = url });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void PrometheusOptions_OneSecondPortfolioRefresh_IsAllowed()
    {
        var result = new PrometheusOptionsValidator().Validate(
            null,
            new PrometheusOptions { PortfolioRefreshSeconds = 1 });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void EventJson_UsesCamelCase()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            new { SignalId = "s1", Allowed = true },
            EventJson.Options);

        Assert.Contains("\"signalId\":\"s1\"", json);
        Assert.Contains("\"allowed\":true", json);
    }
}
