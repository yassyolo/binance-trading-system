using TradingSystem.Domain.Enums;

namespace StrategyService.Bots.Bot8016.Models;

public sealed record Bot8016EntrySignal(
    PositionSide Side,
    Bot8016Candle Candle,
    Bot8016IndicatorSnapshot Indicators,
    string Reason);

