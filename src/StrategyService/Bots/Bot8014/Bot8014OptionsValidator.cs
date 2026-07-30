using Microsoft.Extensions.Options;

namespace StrategyService.Bots.Bot8014;

public sealed class Bot8014OptionsValidator:IValidateOptions<Bot8014Options>
{
    public ValidateOptionsResult Validate(string? n, Bot8014Options x)
    {
        var e = new List<string>();
        
        if(string.IsNullOrWhiteSpace(x.BotName))
            e.Add("BotName is required.");
        
        if(string.IsNullOrWhiteSpace(x.Symbol))
            e.Add("Symbol is required.");
        
        if(x.Quantity<=0)
            e.Add("Quantity must be positive.");
        
        if(x.Leverage is<1 or>125)
            e.Add("Leverage must be between 1 and 125.");
        
        if(x.PriceDistance<=0)
            e.Add("PriceDistance must be positive.");
        
        if(x.ProfitDistance<=0)
            e.Add("ProfitDistance must be positive.");
        
        if(x.OrderSideLimit<=0)
            e.Add("OrderSideLimit must be positive.");
        
        if(x.CooldownSeconds<0)
            e.Add("CooldownSeconds cannot be negative.");
        
        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}
