using TradingSystem.Operations.Configuration;
using TradingSystem.Operations.Models;
using TradingSystem.Operations.Models.Enums;
using Xunit;

namespace TradingSystem.Operations.Tests;

public sealed class OperationsMissingTests
{
    [Fact]
    public void AlertEngineOptions_DefaultOptions_Succeed()
    {
        var result = new AlertEngineOptionsValidator().Validate(null, new AlertEngineOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AlertEngineOptions_NonPositivePollSeconds_Fail(int value)
    {
        var result = new AlertEngineOptionsValidator().Validate(
            null,
            new AlertEngineOptions { PollSeconds = value });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void ServiceHeartbeatOptions_DefaultOptions_Succeed()
    {
        var result = new ServiceHeartbeatOptionsValidator()
            .Validate(null, new ServiceHeartbeatOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ServiceHeartbeatOptions_EmptyServiceName_Fails(string serviceName)
    {
        var options = new ServiceHeartbeatOptions { ServiceName = serviceName };

        var result = new ServiceHeartbeatOptionsValidator().Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ServiceHeartbeatOptions_NonPositiveInterval_Fails(int interval)
    {
        var options = new ServiceHeartbeatOptions { IntervalSeconds = interval };

        var result = new ServiceHeartbeatOptionsValidator().Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ServiceHeartbeatOptions_NonPositiveStaleAfter_Fails(int staleAfter)
    {
        var options = new ServiceHeartbeatOptions { StaleAfterSeconds = staleAfter };

        var result = new ServiceHeartbeatOptionsValidator().Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void ServiceHeartbeatOptions_StaleAfterBelowInterval_Fails()
    {
        var options = new ServiceHeartbeatOptions
        {
            IntervalSeconds = 10,
            StaleAfterSeconds = 9
        };

        var result = new ServiceHeartbeatOptionsValidator().Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void ServiceHeartbeatOptions_StaleAfterEqualToInterval_Succeeds()
    {
        var options = new ServiceHeartbeatOptions
        {
            IntervalSeconds = 10,
            StaleAfterSeconds = 10
        };

        var result = new ServiceHeartbeatOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void AlertCandidate_PreservesOperationalContext()
    {
        var metadata = new Dictionary<string, string>
        {
            ["instanceId"] = "nas-123",
            ["lastSeenUtc"] = "2026-09-07T08:00:00Z"
        };

        var result = new AlertCandidate(
            "operational:heartbeat:StrategyService",
            AlertSeverity.Critical,
            "ServiceHeartbeatMissing",
            "StrategyService is stale.",
            "BOT8012",
            "abc12345",
            metadata);

        Assert.Equal("operational:heartbeat:StrategyService", result.DeduplicationKey);
        Assert.Equal(AlertSeverity.Critical, result.Severity);
        Assert.Equal("ServiceHeartbeatMissing", result.Type);
        Assert.Equal("BOT8012", result.BotName);
        Assert.Equal("abc12345", result.PositionId);
        Assert.Equal("nas-123", result.Metadata!["instanceId"]);
    }

    [Fact]
    public void ServiceHeartbeat_PreservesInstanceAndFreshnessFields()
    {
        var started = new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc);
        var lastSeen = started.AddSeconds(20);

        var result = new ServiceHeartbeat(
            "TradingJobsWorker",
            "nas-42",
            "1.2.3",
            "Demo",
            OperationalStatus.Healthy,
            started,
            lastSeen,
            30,
            new Dictionary<string, string> { ["processId"] = "42" });

        Assert.Equal("TradingJobsWorker", result.ServiceName);
        Assert.Equal("nas-42", result.InstanceId);
        Assert.Equal("Demo", result.Environment);
        Assert.Equal(OperationalStatus.Healthy, result.Status);
        Assert.Equal(started, result.StartedAtUtc);
        Assert.Equal(lastSeen, result.LastSeenAtUtc);
        Assert.Equal(30, result.StaleAfterSeconds);
        Assert.Equal("42", result.Details!["processId"]);
    }

    [Fact]
    public void AuditEvent_PreservesActorCorrelationAndChangeContext()
    {
        var id = Guid.NewGuid();
        var at = new DateTime(2026, 9, 7, 8, 30, 0, DateTimeKind.Utc);

        var result = new AuditEvent(
            id,
            at,
            "operator-a",
            "PATCH /api/v1/bots/BOT8012",
            "HttpRequest",
            "BOT8012",
            "Increase cooldown after review",
            "corr-123",
            "192.168.1.10",
            """{"cooldown":180}""",
            """{"cooldown":240}""",
            new Dictionary<string, string> { ["succeeded"] = "True" });

        Assert.Equal(id, result.AuditId);
        Assert.Equal(at, result.OccurredAtUtc);
        Assert.Equal("operator-a", result.Actor);
        Assert.Equal("BOT8012", result.EntityId);
        Assert.Equal("corr-123", result.CorrelationId);
        Assert.Equal("Increase cooldown after review", result.Reason);
        Assert.Equal("True", result.Metadata!["succeeded"]);
    }
}
