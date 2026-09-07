using TradingSystem.Operations;
using TradingSystem.Operations.Models;
using TradingSystem.Operations.Models.Enums;
using Xunit;

namespace TradingSystem.Operations.Tests; 

public sealed class OperationalContractTests 
{
    [Fact] public void DeduplicationKey_ShouldKeepAlertIdentityStable() 
    { 
        var a = new AlertCandidate("operational:heartbeat:StrategyService:one", AlertSeverity.Critical, "ServiceHeartbeatMissing", "stale"); 
        
        Assert.StartsWith("operational:", a.DeduplicationKey);
    } 
    [Fact] public void Heartbeat_ShouldHaveStaleThreshold()
    { 
        var h = new ServiceHeartbeat("StrategyService", "one", "1.0", "Demo", OperationalStatus.Healthy, DateTime.UtcNow, DateTime.UtcNow, 30); 
        
        Assert.True(h.StaleAfterSeconds > 0); 
    }
}