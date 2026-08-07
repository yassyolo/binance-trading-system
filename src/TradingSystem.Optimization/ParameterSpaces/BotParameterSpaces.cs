using TradingSystem.Backtesting.Bots.Bot8016;
using TradingSystem.Backtesting.Bots.Configuration;

namespace TradingSystem.Optimization.ParameterSpaces;

public static class BotParameterSpaces
{
    public static IEnumerable<Bot8011BacktestOptions> Bot8011(Bot8011BacktestOptions seed)  => 
        from tp in new[] { 0.08m,  0.12m,  0.16m }
        from sl in new[] { 200m,  300m,  400m }
        from step in new[] { 200m,  400m,  600m }
        from buffer in new[] { 25m,  50m,  100m }
        select seed with { TakeProfitPercent  =  tp,  InitialStopLossDistance  =  sl,  Stop3TrailingStep  =  step,  Stop3TrailingBuffer  =  buffer };

    public static IEnumerable<Bot8012BacktestOptions> Bot8012(Bot8012BacktestOptions seed)  => 
        from profit in new[] { 100m,  150m,  200m,  250m,  300m }
        from gap in new[] { 200m,  300m,  400m,  500m,  600m }
        from limit in new[] { 1,  2,  3 }
        select seed with { ProfitDistance  =  profit,  PriceDistance  =  gap,  OrderSideLimit  =  limit };

    public static IEnumerable<Bot8013BacktestOptions> Bot8013(Bot8013BacktestOptions seed)  => 
        from profit in new[] { 100m,  150m,  200m,  250m }
        from gap in new[] { 200m,  300m,  400m,  500m }
        from limit in new[] { 1,  2,  3 }
        select seed with { ProfitDistance  =  profit,  PriceDistance  =  gap,  OrderSideLimit  =  limit };

    public static IEnumerable<Bot8014BacktestOptions> Bot8014(Bot8014BacktestOptions seed)  => 
        from profit in new[] { 100m,  150m,  200m,  250m }
        from gap in new[] { 200m,  300m,  400m,  500m }
        from limit in new[] { 1,  2,  3 }
        select seed with { ProfitDistance  =  profit,  PriceDistance  =  gap,  OrderSideLimit  =  limit };

    public static IEnumerable<Bot8015BacktestOptions> Bot8015(Bot8015BacktestOptions seed)  => 
        from tp in new[] { 0.08m,  0.12m,  0.16m }
        from sl in new[] { 200m,  300m,  400m }
        from step in new[] { 200m,  400m,  600m }
        select seed with { TakeProfitPercent  =  tp,  InitialStopLossDistance  =  sl,  Stop3TrailingStep  =  step };

    public static IEnumerable<Bot8016BacktestOptions> Bot8016(Bot8016BacktestOptions seed)  => 
        from tp in new[] { 0.08m,  0.12m,  0.16m }
        from range in new[] { 25m,  50m,  75m,  100m }
        from ma in new[] { true,  false }
        select seed with { TakeProfitPercent  =  tp,  MinimumSignalCandleRange  =  range,  UseMa200Filter  =  ma };
}
