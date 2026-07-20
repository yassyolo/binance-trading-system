using TradingSystem.Domain.Enums;
using TradingSystem.PaperTrading;
using Xunit;

namespace TradingSystem.PaperTrading.Tests;

public sealed class PaperFillWorkerTests
{
    [Theory]
    [InlineData(PositionSide.Long, 101, "PAPER_TAKE_PROFIT")]
    [InlineData(PositionSide.Long, 98, "PAPER_STOP_LOSS")]
    [InlineData(PositionSide.Short, 99, "PAPER_TAKE_PROFIT")]
    [InlineData(PositionSide.Short, 102, "PAPER_STOP_LOSS")]
    public void ResolveCloseReason_ClosesAtConfiguredBoundary(PositionSide side, decimal price, string expected)
    {
        var p = new PaperTradingPosition(Guid.NewGuid(),"P1","BOT8012","BTCUSDC",side,1,100,side==PositionSide.Long?101:99,side==PositionSide.Long?98:102,0,null,null,null,PaperPositionStatus.Open,"test",DateTime.UtcNow,null,null,1);
        Assert.Equal(expected, PaperFillWorker.ResolveCloseReason(p, price));
    }
}
