using TradingSystem.Application.Risk;

namespace TradingSystem.RiskManagement;

public sealed class CentralRiskOptions
{
    public const string SectionName = "CentralRisk";

    public bool Enabled { get; set; } = true;
    
    public int MaximumOpenPositions { get; set; } = 8;
   
    public int MaximumOpenPositionsPerBot { get; set; } = 4;
   
    public int MaximumOpenPositionsPerSymbol { get; set; } = 6;
   
    public decimal MaximumEstimatedNotional { get; set; } = 100_000m;
    
    public decimal MaximumGrossNotionalPerSymbol { get; set; }
   
    public decimal MaximumAbsoluteNetNotionalPerSymbol { get; set; }
   
    public decimal MaximumDailyLoss { get; set; } = 500m;
    
    public decimal MaximumDailyDrawdownPercent { get; set; } = 5m;
   
    public int MaximumConsecutiveLosses { get; set; } = 5;
   
    public bool BlockWhenReconciliationHasCriticalFindings { get; set; } = true;
    
    public int AdmissionReservationSeconds { get; set; } = 30;
    
    public Dictionary<string, RiskBotProfile> BotProfiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class RiskBotProfile
{
    public decimal Quantity { get; set; }
   
    public int Leverage { get; set; } = 1;
    
    public decimal? MaximumNotional { get; set; }
}

public sealed class EmptyRiskStateProvider : IRiskStateProvider
{
    public Task<RiskStateSnapshot> GetAsync(DateTime atUtc, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new RiskStateSnapshot(0m, 0m, 0m, 0, false));
    }
}



