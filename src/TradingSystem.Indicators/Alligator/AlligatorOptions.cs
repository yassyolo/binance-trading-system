namespace TradingSystem.Indicators.Alligator;

public sealed class AlligatorOptions
{
    public const string SectionName = "Indicators:Alligator";
    
    public bool Enabled { get; set; } = true; 
    
    public string[] Symbols{ get; set; } = ["BTCUSDC"]; 
    
    public string[] Intervals{ get; set; } = ["5m"];
    
    public int HistoryLimit{ get; set; } = 300; 
    
    public int SmaLength{ get; set; } = 200; 
    
    public int JawLength{ get; set; } = 13; 
    
    public int TeethLength{ get; set; } = 8; 
    
    public int LipsLength{ get; set; } = 5;
}
