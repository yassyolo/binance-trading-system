namespace TradingSystem.Strategies.Grid.Models;

public sealed record GridSpacingParameters(
    decimal PriceDistance,
    decimal ProfitDistance,
    int SideLimit);
