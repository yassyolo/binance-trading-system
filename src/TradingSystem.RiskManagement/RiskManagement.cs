using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Risk;
using TradingSystem.Domain.Enums;

namespace TradingSystem.RiskManagement;

public sealed record CentralRiskOptions
{
    public const string SectionName  =  "CentralRisk";
    public bool Enabled {  get;  init;  }  =  true;
    public int MaximumOpenPositions {  get;  init;  }  =  8;
    public int MaximumOpenPositionsPerBot {  get;  init;  }  =  4;
    public int MaximumOpenPositionsPerSymbol {  get;  init;  }  =  6;
    public decimal MaximumEstimatedNotional {  get;  init;  }  =  100_000m;
    public decimal MaximumDailyLoss {  get;  init;  }  =  500m;
    public decimal MaximumDailyDrawdownPercent {  get;  init;  }  =  5m;
    public int MaximumConsecutiveLosses {  get;  init;  }  =  5;
    public bool BlockWhenReconciliationHasCriticalFindings {  get;  init;  }  =  true;
}

public sealed record RiskStateSnapshot(decimal DailyRealizedPnl,  decimal DailyPeakEquity,  decimal CurrentEquity, 
    int ConsecutiveLosses,  bool HasCriticalReconciliationFindings);

public interface IRiskStateProvider
{
    Task<RiskStateSnapshot> GetAsync(DateTime atUtc,  CancellationToken cancellationToken);
}

public sealed class EmptyRiskStateProvider : IRiskStateProvider
{
    public Task<RiskStateSnapshot> GetAsync(DateTime atUtc,  CancellationToken cancellationToken)
         =>  Task.FromResult(new RiskStateSnapshot(0m,  0m,  0m,  0,  false));
}

public sealed class CentralRiskManager(IOptions<CentralRiskOptions> options,  IRiskStateProvider stateProvider) : ICentralRiskManager
{
    private readonly CentralRiskOptions _options  =  options.Value;

    public async Task<RiskDecision> EvaluateOpenAsync(RiskEvaluationContext context,  CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return RiskDecision.Allow("Central risk management is disabled.");
        var positions  =  context.ActivePositions;
        if (positions.Count >= _options.MaximumOpenPositions)
            return RiskDecision.Block("MAX_OPEN_POSITIONS",  $"Open positions {positions.Count}/{_options.MaximumOpenPositions}.");
        var perBot  =  positions.Count(x  =>  x.BotName.Equals(context.Signal.BotName,  StringComparison.OrdinalIgnoreCase));
        if (perBot >= _options.MaximumOpenPositionsPerBot)
            return RiskDecision.Block("MAX_POSITIONS_PER_BOT",  $"Bot positions {perBot}/{_options.MaximumOpenPositionsPerBot}.");
        var perSymbol  =  positions.Count(x  =>  x.Symbol.Equals(context.Signal.Symbol,  StringComparison.OrdinalIgnoreCase));
        if (perSymbol >= _options.MaximumOpenPositionsPerSymbol)
            return RiskDecision.Block("MAX_POSITIONS_PER_SYMBOL",  $"Symbol positions {perSymbol}/{_options.MaximumOpenPositionsPerSymbol}.");
        var estimatedNotional  =  positions.Sum(x  =>  Math.Abs(x.Quantity * (x.EntryPrice > 0 ? x.EntryPrice : context.MarkPrice)));
        if (estimatedNotional >= _options.MaximumEstimatedNotional)
            return RiskDecision.Block("MAX_ESTIMATED_NOTIONAL",  $"Estimated notional {estimatedNotional:F2} exceeds {_options.MaximumEstimatedNotional:F2}.");
        var state  =  await stateProvider.GetAsync(context.EvaluatedAtUtc,  cancellationToken);
        if (state.DailyRealizedPnl <= -_options.MaximumDailyLoss)
            return RiskDecision.Block("DAILY_LOSS_LIMIT",  $"Daily PnL {state.DailyRealizedPnl:F2} reached loss limit.");
        var drawdown  =  state.DailyPeakEquity <= 0 ? 0 : (state.DailyPeakEquity - state.CurrentEquity) / state.DailyPeakEquity * 100m;
        if (drawdown >= _options.MaximumDailyDrawdownPercent)
            return RiskDecision.Block("DAILY_DRAWDOWN_LIMIT",  $"Daily drawdown {drawdown:F2}% reached limit.");
        if (state.ConsecutiveLosses >= _options.MaximumConsecutiveLosses)
            return RiskDecision.Block("CONSECUTIVE_LOSS_LIMIT",  $"Consecutive losses {state.ConsecutiveLosses} reached limit.");
        if (_options.BlockWhenReconciliationHasCriticalFindings  &&  state.HasCriticalReconciliationFindings)
            return RiskDecision.Block("RECONCILIATION_UNHEALTHY",  "Critical unresolved reconciliation findings exist.");
        return RiskDecision.Allow();
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddCentralRiskManagement(this IServiceCollection services,  Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddOptions<CentralRiskOptions>().Bind(configuration.GetSection(CentralRiskOptions.SectionName)).Validate(x  =>  x.MaximumOpenPositions > 0).ValidateOnStart();
        services.TryAddSingleton<IRiskStateProvider,  EmptyRiskStateProvider>();
        services.RemoveAll<ICentralRiskManager>();
        services.AddSingleton<ICentralRiskManager,  CentralRiskManager>();
        return services;
    }
}
