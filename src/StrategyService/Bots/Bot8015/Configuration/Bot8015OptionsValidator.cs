using Microsoft.Extensions.Options;

namespace StrategyService.Bots.Bot8015.Configuration;

public sealed class Bot8015OptionsValidator:IValidateOptions<Bot8015Options>
{
    public ValidateOptionsResult Validate(string? n, Bot8015Options x)
    {
        var e = new List<string>();
        
        if(string.IsNullOrWhiteSpace(x.BotName) || string.IsNullOrWhiteSpace(x.Symbol))
            e.Add("BotName and Symbol are required.");
       
        if(x.Quantity <=0 || x.InitialStopLoss<=0 || x.TpPercent<=0)
            e.Add("Quantity,  InitialStopLoss and TpPercent must be positive.");
        
        if(x.Leverage is <1 or>125)
            e.Add("Leverage must be between 1 and 125.");
       
        if(x.OrderSideLimit <=0 || x.Stop3TrailingStep<=0 || x.Stop3TrailingBuffer<=0)
            e.Add("Limits and trailing values must be positive.");
        
        return e.Count==0?ValidateOptionsResult.Success:ValidateOptionsResult.Fail(e);
    }
}
