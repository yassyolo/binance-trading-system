using Microsoft.Extensions.Options;
using StrategyService.Configuration;

namespace StrategyService.Services;

public sealed class TelegramNotificationService(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramNotificationService> logger)
{
    private readonly TelegramOptions options = options.Value;

    public async Task SendAsync(
        string message,
        CancellationToken cancellationToken)
    {
        if (!options.Enabled ||
            string.IsNullOrWhiteSpace(options.BotToken) ||
            string.IsNullOrWhiteSpace(options.ChatId))
        {
            return;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{options.BotToken}/sendMessage";

            using var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("chat_id", options.ChatId),
                new KeyValuePair<string, string>("text", message)
            ]);

            using var response = await httpClient.PostAsync(
                url,
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Telegram notification failed.");
        }
    }
}
