namespace TradingSystem.Binance.Orders;

public sealed record BinanceOrderResult
{
    public required string Symbol {  get;  init;  }
    public required string ClientOrderId {  get;  init;  }
    public required string OrderId {  get;  init;  }
    public string? Status {  get;  init;  }
    public decimal? AveragePrice {  get;  init;  }
    public decimal? ExecutedQuantity {  get;  init;  }
    public decimal? CumulativeQuoteQuantity {  get;  init;  }
}

public sealed record BinanceAlgoOrderResult
{
    public required string Symbol {  get;  init;  }
    public required string ClientOrderId {  get;  init;  }
    public required string AlgoOrderId {  get;  init;  }
    public string? Status {  get;  init;  }
}

public sealed record BinanceOpenOrder
{
    public string Symbol {  get;  init;  }  =  string.Empty;
    public string OrderId {  get;  init;  }  =  string.Empty;
    public string ClientOrderId {  get;  init;  }  =  string.Empty;
    public string Type {  get;  init;  }  =  string.Empty;
    public string Side {  get;  init;  }  =  string.Empty;
    public string PositionSide {  get;  init;  }  =  string.Empty;
    public decimal Price {  get;  init;  }
    public decimal Quantity {  get;  init;  }
    public DateTime CreatedAtUtc {  get;  init;  }
    public DateTime UpdateTimeUtc {  get;  init;  }
}

public sealed record BinanceOpenAlgoOrder
{
    public string Symbol {  get;  init;  }  =  string.Empty;
    public string AlgoOrderId {  get;  init;  }  =  string.Empty;
    public string ClientAlgoId {  get;  init;  }  =  string.Empty;
    public string PositionSide {  get;  init;  }  =  string.Empty;
    public string Status {  get;  init;  }  =  string.Empty;
    public decimal TriggerPrice {  get;  init;  }
    public decimal Quantity {  get;  init;  }
    public string OrderType {  get;  init;  }  =  string.Empty;
}

public sealed record BinanceSymbolFilters
{
    public decimal TickSize {  get;  init;  }
    public decimal StepSize {  get;  init;  }
    public decimal MinQuantity {  get;  init;  }
}
