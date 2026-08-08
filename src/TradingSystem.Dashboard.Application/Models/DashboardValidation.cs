using TradingSystem.Dashboard.Contracts.Models.Backtesting;
using TradingSystem.Dashboard.Contracts.Models.Bots;
using TradingSystem.Dashboard.Contracts.Models.Enums;

namespace TradingSystem.Dashboard.Application.Models;

public static class DashboardValidation
{
    public static void Validate(UpdateBotConfigurationRequest r)
    {
        if (r.ExpectedVersion < 1) 
            throw new ArgumentException("ExpectedVersion is required.");
        
        if (string.IsNullOrWhiteSpace(r.StrategyType)) 
            throw new ArgumentException("StrategyType is required.");
        
        if (string.IsNullOrWhiteSpace(r.Symbol)) 
            throw new ArgumentException("Symbol is required.");
        
        if (r.Quantity <= 0) 
            throw new ArgumentException("Quantity must be positive.");
        
        if (r.Leverage is < 1 or > 125) 
            throw new ArgumentException("Leverage must be between 1 and 125.");
        
        if (r.CooldownSeconds < 0) 
            throw new ArgumentException("CooldownSeconds cannot be negative.");
        
        if (r.PriceDistance <= 0 && r.PriceDistance is not null) 
            throw new ArgumentException("PriceDistance must be positive.");
        
        if (r.ProfitDistance <= 0 && r.ProfitDistance is not null) 
            throw new ArgumentException("ProfitDistance must be positive.");
       
        if (r.OrderSideLimit <= 0 && r.OrderSideLimit is not null) 
            throw new ArgumentException("OrderSideLimit must be positive.");
       
        if (string.IsNullOrWhiteSpace(r.Reason)) 
            throw new ArgumentException("A change reason is required.");
    }
    
    public static void Validate(BotCommandRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Reason)) 
            throw new ArgumentException("A command reason is required.");
       
        if (r.Command is BotCommandType.EmergencyStop 
            or BotCommandType.ClosePosition 
            or BotCommandType.CancelTakeProfit 
            or BotCommandType.RecreateTakeProfit 
            && !r.Confirmed)
            throw new ArgumentException("This trading action requires explicit confirmation.");
        
        if (r.Command is BotCommandType.ClosePosition 
            or BotCommandType.CancelTakeProfit 
            or BotCommandType.RecreateTakeProfit 
            && string.IsNullOrWhiteSpace(r.PositionId))
            throw new ArgumentException("PositionId is required.");
    }
    
    public static void Validate(BacktestRequest r)
    {
        if (r.ToUtc <= r.FromUtc) 
            throw new ArgumentException("Backtest period is invalid.");
        
        if (r.InitialBalance <= 0) 
            throw new ArgumentException("Initial balance must be positive.");
        
        if (r.CommissionPercent < 0 || r.SlippagePercent < 0) 
            throw new ArgumentException("Costs cannot be negative.");
    }
}
