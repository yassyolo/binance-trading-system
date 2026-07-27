using TradingSystem.Application.Orders;using TradingSystem.Application.Positions;using TradingSystem.Application.Time;
namespace StrategyService.Bots.Common.TpOnlyGrid;
public abstract class TpOnlyGridOrderEventHandler<TOptions>(TOptions options, IPositionStore store, IClock clock):IBotOrderEventHandler where TOptions:class, ITpOnlyGridBotOptions
{
 public string BotName => options.BotName;
 public async Task HandleTpFilledAsync(string id, decimal qty, CancellationToken ct){var p = await store.GetAsync(BotName, id, ct);if(p is null || p.Closed)return;p.MarkTpFilled(qty, clock.UtcNow);await store.SaveAsync(p, ct);}
 public async Task HandleTpTerminalAsync(string id, string status, CancellationToken ct)
{
    var p = await store.GetAsync(BotName, id, ct);
    if (p is null || p.Closed) return;
    p.MarkTpOrderTerminal(status, clock.UtcNow);
    await store.SaveAsync(p, ct);
}
 public Task HandleSlTriggeredAsync(string id, CancellationToken ct) => Task.CompletedTask;public Task HandleStop3TriggeredAsync(string id, CancellationToken ct) => Task.CompletedTask;
}
