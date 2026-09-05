using Microsoft.Extensions.Options;

namespace StrategyService.Bots.Bot8014.Configuration;

public sealed class Bot8014OptionsValidator : IValidateOptions<Bot8014Options>
{
    public ValidateOptionsResult Validate(string? name, Bot8014Options options)
    {
        var e = new List<string>();
        
        if(string.IsNullOrWhiteSpace(options.BotName))
            e.Add("BotName is required.");
        
        if(string.IsNullOrWhiteSpace(options.Symbol))
            e.Add("Symbol is required.");
        
        if(options.Quantity <= 0)
            e.Add("Quantity must be positive.");
        
        if(options.Leverage is < 1 or > 125)
            e.Add("Leverage must be between 1 and 125.");
        
        if(options.PriceDistance <= 0)
            e.Add("PriceDistance must be positive.");
        
        if(options.ProfitDistance <= 0)
            e.Add("ProfitDistance must be positive.");
        
        if(options.OrderSideLimit <= 0)
            e.Add("OrderSideLimit must be positive.");
        
        if(options.CooldownSeconds < 0)
            e.Add("CooldownSeconds cannot be negative.");
        
        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}
