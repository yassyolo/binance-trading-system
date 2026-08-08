using TradingSystem.Domain.Enums;
using TradingSystem.Strategies.Alligator;
using TradingSystem.Strategies.Grid;
using TradingSystem.Strategies.Protection;
using Xunit;

namespace TradingSystem.Strategies.Tests; 

public sealed class PolicyTests
{ 
    [Fact] public void Grid_long_requires_distance() 
    { var p = new GridSpacingPolicy(); var d = p.Evaluate(PositionSide.Long, 49_500m, [new(PositionSide.Long, 50_200m, DateTime.UtcNow)], new(400m, 200m, 2)); Assert.True(d.Allowed); } [Fact] public void Stop3_long_moves_by_buffer_after_step() { var p = new Stop3Policy(); Assert.Equal(50_050m, p.NextTrigger(PositionSide.Long, 50_400m, 50_000m, new(0, 400, 50))); } [Fact] public void Alligator_policy_returns_long() { var p = new AlligatorEntryPolicy(); var d = p.Evaluate(new("BTCUSDC", "5m", true, 100, 180, 90, 160, 120, 150), new("BTCUSDC", "5m", true, true, true, 50)); Assert.Equal(PositionSide.Long, d!.Side); } }