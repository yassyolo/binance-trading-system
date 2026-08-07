using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012.Configuration;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Domain.Enums;
namespace StrategyService.Bots.Bot8012;
public sealed class Bot8012Strategy(IOptions<Bot8012Options> options, Bot8012GapPolicy policy):ITradingStrategy, IHasSignalCooldown
{private readonly Bot8012Options _o = options.Value;public StrategyMetadata Metadata => new(_o.BotName, _o.StrategyVersion, PositionMode.TpOnly, [_o.Symbol]);public TimeSpan SignalCooldown => TimeSpan.FromSeconds(_o.CooldownSeconds);public Task<StrategyDecision> DecideAsync(StrategyContext c, CancellationToken ct){ct.ThrowIfCancellationRequested();var side = c.Signal.Side;if(side==PositionSide.Long && !_o.EnableLong)return Task.FromResult(StrategyDecision.Block(side, "LONG is disabled."));if(side==PositionSide.Short && !_o.EnableShort)return Task.FromResult(StrategyDecision.Block(side, "SHORT is disabled."));return Task.FromResult(policy.Evaluate(side,  c.MarkPrice,  c.ActivePositions,  c.RuntimeConfiguration));}}
