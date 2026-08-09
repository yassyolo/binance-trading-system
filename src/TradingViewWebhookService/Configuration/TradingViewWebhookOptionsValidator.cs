using Microsoft.Extensions.Options;

namespace TradingViewWebhookService.Configuration;

public sealed class TradingViewWebhookOptionsValidator
    : IValidateOptions<TradingViewWebhookOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        TradingViewWebhookOptions options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Secret))
            errors.Add("TradingView:Secret is required when TradingView webhook ingress is enabled.");

        if (options.Bots is null || options.Bots.Count == 0)
        {
            errors.Add("TradingView:Bots must contain at least one bot.");
        }
        else
        {
            foreach (var (botName, bot) in options.Bots)
            {
                if (string.IsNullOrWhiteSpace(botName))
                    errors.Add("TradingView:Bots contains an empty bot name.");

                if (bot is null)
                {
                    errors.Add($"TradingView:Bots:{botName} is required.");
                    continue;
                }

                if (bot.Enabled && string.IsNullOrWhiteSpace(bot.Symbol))
                    errors.Add($"TradingView:Bots:{botName}:Symbol is required when enabled.");
            }
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
