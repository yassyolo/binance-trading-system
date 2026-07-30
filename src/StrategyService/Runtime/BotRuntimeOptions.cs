namespace StrategyService.Runtime;

public sealed class BotRuntimeOptions
{
    public const string SectionName  =  "BotRuntime";
    
    public bool Enabled {  get;  set; }  =  true;
    
    public int CommandPollSeconds {  get;  set; }  =  2;
   
    public int CommandBatchSize {  get;  set; }  =  20;
   
    public int CommandProcessingTimeoutSeconds {  get;  set; }  =  60;
   
    public int ConfigurationRefreshSeconds {  get;  set; }  =  5;
    
    public int MaximumCommandAttempts {  get;  set; }  =  5;
}
