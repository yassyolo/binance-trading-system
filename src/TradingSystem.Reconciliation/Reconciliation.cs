using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingSystem.Domain.Positions;

namespace TradingSystem.Reconciliation;

public enum ReconciliationSeverity { Information,  Warning,  Critical }
public enum ReconciliationFindingType { StaleLocalPosition,  MissingLocalPosition,  MissingTakeProfit,  MissingStopLoss,  QuantityMismatch,  OrphanExchangePosition,  OrphanExchangeOrder }
public enum HealingActionType { None,  DeleteStaleLocalPosition,  RestoreLocalPosition,  RecreateTakeProfit,  RecreateStopLoss,  MarkClosed,  RequestManualReview }

public sealed record ExchangePositionSnapshot(string Symbol,  string Side,  decimal Quantity,  decimal EntryPrice);
public sealed record ExchangeOrderSnapshot(string Symbol,  string ClientOrderId,  string Kind,  decimal Quantity,  decimal? TriggerPrice);
public sealed record ExchangeStateSnapshot(IReadOnlyCollection<ExchangePositionSnapshot> Positions,  IReadOnlyCollection<ExchangeOrderSnapshot> Orders);
public sealed record ReconciliationFinding(Guid Id,  DateTime DetectedAtUtc,  string BotName,  string Symbol,  string? ShortId, 
    ReconciliationFindingType Type,  ReconciliationSeverity Severity,  string Details,  HealingActionType SuggestedAction,  bool AutoHealAllowed);
public sealed record ReconciliationRunResult(DateTime StartedAtUtc,  DateTime CompletedAtUtc,  IReadOnlyCollection<ReconciliationFinding> Findings,  int HealedCount);

public interface IExchangeStateProvider { Task<ExchangeStateSnapshot> GetAsync(string symbol,  CancellationToken cancellationToken); }
public interface IReconciliationFindingStore
{
    Task SaveRunAsync(ReconciliationRunResult result,  CancellationToken cancellationToken);
    Task<bool> HasUnresolvedCriticalAsync(CancellationToken cancellationToken);
}
public interface IHealingActionExecutor { Task<bool> ExecuteAsync(ReconciliationFinding finding,  CancellationToken cancellationToken); }

public sealed record ReconciliationOptions
{
    public const string SectionName  =  "Reconciliation";
    public bool Enabled {  get;  init;  }  =  true;
    public int IntervalSeconds {  get;  init;  }  =  30;
    public bool AutoHealStaleLocalPositions {  get;  init;  }  =  true;
    public bool AutoHealProtectiveOrders {  get;  init;  }
    public decimal QuantityTolerance {  get;  init;  }  =  0.00000001m;
    public string[] Bots {  get;  init;  }  =  ["BOT8011", "BOT8012", "BOT8013", "BOT8014", "BOT8015", "BOT8016"];
    public string[] Symbols {  get;  init;  }  =  ["BTCUSDC"];
}

public sealed class PositionReconciliationService(IOptions<ReconciliationOptions> options, 
    TradingSystem.Application.Positions.IPositionStore localStore,  IExchangeStateProvider exchange, 
    IHealingActionExecutor healer,  IReconciliationFindingStore findingStore)
{
    private readonly ReconciliationOptions _options  =  options.Value;
    public async Task<ReconciliationRunResult> RunAsync(CancellationToken cancellationToken)
    {
        var started  =  DateTime.UtcNow; var findings  =  new List<ReconciliationFinding>(); var healed  =  0;
        foreach (var symbol in _options.Symbols)
        {
            var remote  =  await exchange.GetAsync(symbol,  cancellationToken);
            var allLocal  =  new List<BotPosition>();
            foreach (var bot in _options.Bots)
            {
                var local  =  (await localStore.GetAllAsync(bot,  cancellationToken)).Where(x  =>  x.Symbol.Equals(symbol,  StringComparison.OrdinalIgnoreCase)  &&  !x.Closed).ToArray();
                allLocal.AddRange(local);
                foreach (var p in local)
                {
                    var relatedOrders  =  remote.Orders.Where(x  =>  Matches(p,  x.ClientOrderId)).ToArray();
                    var remotePosition  =  remote.Positions.FirstOrDefault(x  =>  x.Symbol.Equals(p.Symbol,  StringComparison.OrdinalIgnoreCase)  &&  x.Side.Equals(p.Side.ToString(),  StringComparison.OrdinalIgnoreCase)  &&  x.Quantity > 0);
                    if (remotePosition is null  &&  relatedOrders.Length == 0)
                        findings.Add(New(p,  ReconciliationFindingType.StaleLocalPosition,  ReconciliationSeverity.Warning,  "Local position has no exchange position or open orders.",  HealingActionType.DeleteStaleLocalPosition,  _options.AutoHealStaleLocalPositions));
                    if (remotePosition is not null  &&  Math.Abs(remotePosition.Quantity - p.RemainingQuantity) > _options.QuantityTolerance)
                        findings.Add(New(p,  ReconciliationFindingType.QuantityMismatch,  ReconciliationSeverity.Critical,  $"Local remaining = {p.RemainingQuantity},  exchange = {remotePosition.Quantity}.",  HealingActionType.RequestManualReview,  false));
                    if (p.TpClientId is not null  &&  relatedOrders.All(x  =>  !x.ClientOrderId.Equals(p.TpClientId,  StringComparison.OrdinalIgnoreCase))  &&  !p.TpExecuted)
                        findings.Add(New(p,  ReconciliationFindingType.MissingTakeProfit,  ReconciliationSeverity.Critical,  "Expected take-profit order is missing.",  HealingActionType.RecreateTakeProfit,  _options.AutoHealProtectiveOrders));
                    if (p.ProtectiveActive  &&  p.SlClientId is not null  &&  relatedOrders.All(x  =>  !x.ClientOrderId.Equals(p.SlClientId,  StringComparison.OrdinalIgnoreCase))  &&  !p.SlExecuted)
                        findings.Add(New(p,  ReconciliationFindingType.MissingStopLoss,  ReconciliationSeverity.Critical,  "Expected stop-loss order is missing.",  HealingActionType.RecreateStopLoss,  _options.AutoHealProtectiveOrders));
                }
            }
            foreach (var order in remote.Orders.Where(x  =>  x.ClientOrderId.StartsWith("BOT",  StringComparison.OrdinalIgnoreCase)))
            {
                if (allLocal.All(x  =>  !Matches(x,  order.ClientOrderId)))
                    findings.Add(new ReconciliationFinding(Guid.NewGuid(),  DateTime.UtcNow,  ResolveBot(order.ClientOrderId),  symbol,  null, 
                        ReconciliationFindingType.OrphanExchangeOrder,  ReconciliationSeverity.Critical, 
                        $"Exchange order '{order.ClientOrderId}' is not represented in local state.",  HealingActionType.RequestManualReview,  false));
            }
            foreach (var position in remote.Positions)
            {
                var matching  =  allLocal.Any(x  =>  x.Symbol.Equals(position.Symbol,  StringComparison.OrdinalIgnoreCase)  && 
                    x.Side.ToString().Equals(position.Side,  StringComparison.OrdinalIgnoreCase));
                if (!matching)
                    findings.Add(new ReconciliationFinding(Guid.NewGuid(),  DateTime.UtcNow,  "UNKNOWN",  symbol,  null, 
                        ReconciliationFindingType.OrphanExchangePosition,  ReconciliationSeverity.Critical, 
                        $"Exchange {position.Side} position quantity = {position.Quantity} has no local owner.",  HealingActionType.RequestManualReview,  false));
            }
        }
        foreach (var f in findings.Where(x  =>  x.AutoHealAllowed)) if (await healer.ExecuteAsync(f,  cancellationToken)) healed++;
        var result  =  new ReconciliationRunResult(started,  DateTime.UtcNow,  findings,  healed);
        await findingStore.SaveRunAsync(result,  cancellationToken); return result;
    }
    private static bool Matches(BotPosition p,  string id)  =>  new[]{p.ParentClientId, p.TpClientId, p.SlClientId, p.Stop3ClientId, p.CloseClientId}.Any(x  =>  x is not null  &&  x.Equals(id, StringComparison.OrdinalIgnoreCase));
    private static string ResolveBot(string clientOrderId)
    {
        var separator  =  clientOrderId.IndexOf('_');
        return separator > 0 ? clientOrderId[..separator] : clientOrderId.Length >= 7 ? clientOrderId[..7] : "UNKNOWN";
    }
    private static ReconciliationFinding New(BotPosition p,  ReconciliationFindingType t,  ReconciliationSeverity s,  string details,  HealingActionType action,  bool auto)
         =>  new(Guid.NewGuid(),  DateTime.UtcNow,  p.BotName,  p.Symbol,  p.ShortId,  t,  s,  details,  action,  auto);
}

public static class DependencyInjection
{
    public static IServiceCollection AddTradingReconciliation(this IServiceCollection services,  Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddOptions<ReconciliationOptions>().Bind(configuration.GetSection(ReconciliationOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<PositionReconciliationService>(); return services;
    }
}
