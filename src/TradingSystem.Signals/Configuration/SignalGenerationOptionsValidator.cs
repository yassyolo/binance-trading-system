using Microsoft.Extensions.Options;

namespace TradingSystem.Signals.Configuration;

public sealed class SignalGenerationOptionsValidator : IValidateOptions<SignalGenerationOptions>
{
    public ValidateOptionsResult Validate(string? name, SignalGenerationOptions options)
    {
        var e = new List<string>();
       
        if (options.Bots is null)
            e.Add("SignalGeneration:Bots is required.");
        else
        {
            foreach (var (botName, bot) in options.Bots)
            {
                if (string.IsNullOrWhiteSpace(botName))
                    e.Add("SignalGeneration:Bots contains an empty bot name.");
                
                if (bot is null)
                {
                    e.Add($"SignalGeneration:Bots:{botName} is required.");
                    continue;
                }
               
                if (bot.Enabled && string.IsNullOrWhiteSpace(bot.Symbol))
                    e.Add($"SignalGeneration:Bots:{botName}:Symbol is required when enabled.");
                
                if (bot.Enabled && string.IsNullOrWhiteSpace(bot.Interval))
                    e.Add($"SignalGeneration:Bots:{botName}:Interval is required when enabled.");
                
                if (bot.MinimumSecondsBetweenGeneratedSignals is < 0 or > 86_400)
                    e.Add($"SignalGeneration:Bots:{botName}:MinimumSecondsBetweenGeneratedSignals must be between 0 and 86400.");
            }
        }

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}
