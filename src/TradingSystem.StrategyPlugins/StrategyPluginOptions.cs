namespace TradingSystem.StrategyPlugins;

public sealed class StrategyPluginOptions
{
    public const string SectionName  =  "StrategyPlugins";

    public bool Enabled {  get;  set; }  =  true;

    public string PluginDirectory {  get;  set; }  =  "plugins/strategies";

    public bool LoadExternalAssemblies {  get;  set; }  =  true;

    public bool FailOnPluginLoadError {  get;  set; }  =  true;

    public string DefaultVersion {  get;  set; }  =  "1.0.0";
}
