namespace TradingSystem.PaperTrading;

public sealed class PaperTradingOptions
{
    public const string SectionName  =  "PaperTrading";
    public bool Enabled {  get;  set; }  =  true;
    public decimal InitialBalance {  get;  set; }  =  10_000m;
    public decimal CommissionPercent {  get;  set; }  =  0.04m;
    public decimal SlippagePercent {  get;  set; }  =  0.01m;
    public decimal DefaultTakeProfitPercent {  get;  set; }  =  0.50m;
    public decimal DefaultStopLossPercent {  get;  set; }  =  0.75m;
    public int PricePollMilliseconds {  get;  set; }  =  1000;
    public int MaximumOpenPositions {  get;  set; }  =  100;
}
