using Microsoft.Extensions.Options;using TradingSystem.Application.Risk;using TradingSystem.Domain.Signals;using TradingSystem.Domain.Enums;using TradingSystem.RiskManagement;using Xunit;
public sealed class CentralRiskManagerTests{
 [Fact]public async Task Blocks_WhenDailyLossLimitReached(){var m=new CentralRiskManager(Options.Create(new CentralRiskOptions{MaximumDailyLoss=100}),new State(new(-100,1000,900,0,false)));var d=await m.EvaluateOpenAsync(Context(),default);Assert.False(d.Allowed);Assert.Equal("DAILY_LOSS_LIMIT",d.Code);}
 private static RiskEvaluationContext Context()=>new(){Signal=new TradeSignal{SignalId="s",BotName="BOT8012",Symbol="BTCUSDC",Side=PositionSide.Long,Source="test",GeneratedAtUtc=DateTime.UtcNow},MarkPrice=100,ActivePositions=[],EvaluatedAtUtc=DateTime.UtcNow};
 private sealed class State(RiskStateSnapshot s):IRiskStateProvider{public Task<RiskStateSnapshot> GetAsync(DateTime a,CancellationToken c)=>Task.FromResult(s);}}
