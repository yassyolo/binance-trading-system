using Microsoft.Extensions.Options;
using StrategyService.Configuration;

namespace StrategyService.Services;

public sealed class TelegramNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramNotificationService> _logger;

    public TelegramNotificationService(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramNotificationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string message,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled ||
            string.IsNullOrWhiteSpace(_options.BotToken) ||
            string.IsNullOrWhiteSpace(_options.ChatId))
        {
            return;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";

            using var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("chat_id", _options.ChatId),
                new KeyValuePair<string, string>("text", message)
            ]);

            using var response = await _httpClient.PostAsync(
                url,
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram notification failed.");
        }
    }
}
