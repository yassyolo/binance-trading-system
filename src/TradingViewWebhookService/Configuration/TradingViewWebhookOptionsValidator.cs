using Microsoft.Extensions.Options;

namespace TradingViewWebhookService.Configuration;

public sealed class TradingViewWebhookOptionsValidator : IValidateOptions<TradingViewWebhookOptions>
{
    public ValidateOptionsResult Validate(string? name, TradingViewWebhookOptions options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        var e = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Secret))
            e.Add("TradingView:Secret is required when TradingView webhook ingress is enabled.");

        if (options.Bots is null || options.Bots.Count == 0)
        {
            e.Add("TradingView:Bots must contain at least one bot.");
        }
        else
        {
            foreach (var (botName, bot) in options.Bots)
            {
                if (string.IsNullOrWhiteSpace(botName))
                    e.Add("TradingView:Bots contains an empty bot name.");

                if (bot is null)
                {
                    e.Add($"TradingView:Bots:{botName} is required.");
                    continue;
                }

                if (bot.Enabled && string.IsNullOrWhiteSpace(bot.Symbol))
                    e.Add($"TradingView:Bots:{botName}:Symbol is required when enabled.");
            }
        }

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}
