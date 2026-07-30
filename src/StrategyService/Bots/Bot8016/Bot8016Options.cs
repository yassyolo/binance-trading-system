using TradingSystem.Binance.Startup;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016Options : IBinanceTradingConfiguration
{
    public const string SectionName  =  "Bots:Bot8016";

    public string BotName {  get;  set; }  =  "BOT8016";
    
    public string StrategyVersion {  get;  set; }  =  "1.0.0";
    
    public string Symbol {  get;  set; }  =  "BTCUSDC";

    public decimal Quantity {  get;  set; }  =  0.002m;
    
    public int Leverage {  get;  set; }  =  50;
 
    public decimal InitialStopLossFallback {  get;  set; }  =  300m;
    
    public decimal TpPercent {  get;  set; }  =  0.12m;
   
    public decimal Stop3EntryOffset {  get;  set; }  =  0m;

    public int PositionSideLimit {  get;  set; }  =  1;
   
    public bool UseMa200Filter {  get;  set; }  =  true;
    
    public decimal MinimumSignalCandleRange {  get;  set; }  =  50m;

    public string EntryTimeframe {  get;  set; }  =  "5m";
   
    public string ExitTimeframe {  get;  set; }  =  "1m";

    public string AlligatorChannel {  get;  set; }  =  "indicator_channel:alligator_ma";
    
    public int AlligatorMaxAgeSeconds {  get;  set; }  =  30;

    public bool EnableLong {  get;  set; }  =  true;
   
    public bool EnableShort {  get;  set; }  =  true;
    
    public bool EnableHealing {  get;  set; }  =  true;
    
    public int HealingIntervalSeconds {  get;  set; }  =  10;
}
