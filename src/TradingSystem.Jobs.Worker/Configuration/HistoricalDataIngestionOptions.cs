namespace TradingSystem.Jobs.Worker.Configuration;

public sealed record HistoricalDataIngestionOptions
{
    public const string SectionName = "HistoricalDataIngestion";
    
    public bool Enabled { get; init; } = true;
   
    public string Environment { get; init; } = "Demo";
   
    public string[] Symbols { get; init; } = ["BTCUSDC"];
   
    public string[] Intervals { get; init; } = ["1m", "5m", "1h"];
    
    public int PollMinutes { get; init; } = 5;
    
    public int InitialLookbackDays { get; init; } = 30;
    
    public int OverlapCandles { get; init; } = 3;
}

