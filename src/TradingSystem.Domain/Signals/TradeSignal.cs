using TradingSystem.Domain.Enums;

namespace TradingSystem.Domain.Signals;

public sealed record TradeSignal
{
    public required string SignalId {  get;  init;  }
    public required string BotName {  get;  init;  }
    public required string Symbol {  get;  init;  }
    public required PositionSide Side {  get;  init;  }
    public required string Source {  get;  init;  }
    public required DateTime GeneratedAtUtc {  get;  init;  }
    public decimal? SuggestedPrice {  get;  init;  }
    public string? RawPayload {  get;  init;  }
    public IReadOnlyDictionary<string,  string> Metadata {  get;  init;  }  =  new Dictionary<string,  string>();
}
