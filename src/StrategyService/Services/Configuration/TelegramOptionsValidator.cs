using Microsoft.Extensions.Options;

namespace StrategyService.Services.Configuration;

public sealed class TelegramOptionsValidator
    : IValidateOptions<TelegramOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        TelegramOptions options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BotToken))
        {
            errors.Add(
                "BotToken is required when Telegram notifications are enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.ChatId))
        {
            errors.Add(
                "ChatId is required when Telegram notifications are enabled.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}