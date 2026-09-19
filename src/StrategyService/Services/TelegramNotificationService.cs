using Microsoft.Extensions.Options;
using StrategyService.Services.Configuration;

namespace StrategyService.Services;

public sealed class TelegramNotificationService(
    HttpClient http, 
    IOptions<TelegramOptions> options, 
    ILogger<TelegramNotificationService> logger)
{
    private readonly TelegramOptions _options = options.Value;

    public async Task SendAsync(string message, CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            logger.LogWarning("Telegram notifications are disabled.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            logger.LogWarning("Telegram BotToken is missing.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ChatId))
        {
            logger.LogWarning("Telegram ChatId is missing.");
            return;
        }

        try
        {
            using var body = new FormUrlEncodedContent([new("chat_id", _options.ChatId), new("text", message) ]);

            using var response = await http.PostAsync($"https://api.telegram.org/bot{_options.BotToken}/sendMessage", body, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Telegram returned HTTP {StatusCode}. Response: {Response}", (int)response.StatusCode, responseBody);
                return;
            }

            logger.LogInformation("Telegram notification sent successfully.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Telegram notification failed.");
        }
    }
}
