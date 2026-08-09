using Microsoft.Extensions.Options;

namespace StrategyService.Services.Configuration;

public sealed class TelegramOptionsValidator : IValidateOptions<TelegramOptions>
{
    public ValidateOptionsResult Validate(string? name, TelegramOptions options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        var e = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BotToken))
            e.Add("BotToken is required when Telegram notifications are enabled.");
       
        if (string.IsNullOrWhiteSpace(options.ChatId))
            e.Add("ChatId is required when Telegram notifications are enabled.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}