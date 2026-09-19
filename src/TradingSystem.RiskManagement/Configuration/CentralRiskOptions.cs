using TradingSystem.RiskManagement.Models;

namespace TradingSystem.RiskManagement.Configuration;

public sealed class CentralRiskOptions
{
    public const string SectionName = "CentralRisk";

    public bool Enabled { get; set; } = true;
    
    public int MaximumOpenPositions { get; set; } = 8;
   
    public int MaximumOpenPositionsPerBot { get; set; } = 4;
   
    public int MaximumOpenPositionsPerSymbol { get; set; } = 6;
   
    public decimal MaximumEstimatedNotional { get; set; } = 100_000m;
          
    public decimal MaximumDailyLoss { get; set; } = 500m;
    
    public decimal MaximumDailyDrawdownPercent { get; set; } = 5m;
   
    public int MaximumConsecutiveLosses { get; set; } = 5;
   
    public bool BlockWhenReconciliationHasCriticalFindings { get; set; } = true;
    
    public int AdmissionReservationSeconds { get; set; } = 30;
    
    public Dictionary<string, RiskBotProfile> BotProfiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}