using TradingSystem.Domain.Enums;
using TradingSystem.Strategies.Grid;
using TradingSystem.Strategies.Grid.Models;
using Xunit;

namespace TradingSystem.Strategies.Tests;

public sealed class GridSpacingPolicyBranchTests
{
    private readonly GridSpacingPolicy _sut = new();

    [Fact]
    public void Evaluate_WhenNoSameSidePositions_Allows()
    {
        var result = _sut.Evaluate(
            PositionSide.Long,
            50_000m,
            [],
            Parameters());

        Assert.True(result.Allowed);
    }

    [Fact]
    public void Evaluate_WhenSideLimitReached_Blocks()
    {
        var result = _sut.Evaluate(
            PositionSide.Long,
            49_000m,
            [
                Position(PositionSide.Long, 50_200m, 2),
                Position(PositionSide.Long, 49_800m, 1)
            ],
            Parameters() with { SideLimit = 2 });

        Assert.False(result.Allowed);
        Assert.Contains(
            "ORDER_SIDE_LIMIT",
            result.Reason,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_Long_AtExactSpacingBoundary_Allows()
    {
        var result = _sut.Evaluate(
            PositionSide.Long,
            49_600m,
            [Position(PositionSide.Long, 50_200m, 1)],
            new GridSpacingParameters(
                400m,
                200m,
                2));

        Assert.True(result.Allowed);
    }

    [Fact]
    public void Evaluate_Long_AboveSpacingBoundary_Blocks()
    {
        var result = _sut.Evaluate(
            PositionSide.Long,
            49_601m,
            [Position(PositionSide.Long, 50_200m, 1)],
            new GridSpacingParameters(
                400m,
                200m,
                2));

        Assert.False(result.Allowed);
    }

    [Fact]
    public void Evaluate_Short_AtExactSpacingBoundary_Allows()
    {
        var result = _sut.Evaluate(
            PositionSide.Short,
            50_600m,
            [Position(PositionSide.Short, 50_000m, 1)],
            new GridSpacingParameters(
                400m,
                200m,
                2));

        Assert.True(result.Allowed);
    }

    [Fact]
    public void Evaluate_Short_BelowSpacingBoundary_Blocks()
    {
        var result = _sut.Evaluate(
            PositionSide.Short,
            50_599m,
            [Position(PositionSide.Short, 50_000m, 1)],
            new GridSpacingParameters(
                400m,
                200m,
                2));

        Assert.False(result.Allowed);
    }

    [Fact]
    public void Evaluate_OppositeSidePositions_DoNotConsumeSideLimit()
    {
        var result = _sut.Evaluate(
            PositionSide.Long,
            50_000m,
            [
                Position(PositionSide.Short, 50_000m, 3),
                Position(PositionSide.Short, 50_100m, 2),
                Position(PositionSide.Short, 50_200m, 1)
            ],
            Parameters() with { SideLimit = 1 });

        Assert.True(result.Allowed);
    }

    private static GridPositionReference Position(
        PositionSide side,
        decimal takeProfit,
        int secondsAgo) =>
        new(
            side,
            takeProfit,
            DateTime.UtcNow.AddSeconds(-secondsAgo));

    private static GridSpacingParameters Parameters() =>
        new(
            400m,
            200m,
            2);
}
