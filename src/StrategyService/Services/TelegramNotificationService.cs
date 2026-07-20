using Microsoft.Extensions.Logging;using Microsoft.Extensions.Options;
namespace StrategyService.Services;
public sealed class TelegramNotificationService(HttpClient http, IOptions<TelegramOptions> options, ILogger<TelegramNotificationService> logger)
{private readonly TelegramOptions _o = options.Value;public async Task SendAsync(string message, CancellationToken ct){if(!_o.Enabled || string.IsNullOrWhiteSpace(_o.BotToken) || string.IsNullOrWhiteSpace(_o.ChatId))return;try{using var body = new FormUrlEncodedContent([new("chat_id", _o.ChatId), new("text", message)]);using var response = await http.PostAsync($"https://api.telegram.org/bot{_o.BotToken}/sendMessage", body, ct);response.EnsureSuccessStatusCode();}catch(Exception ex){logger.LogWarning(ex, "Telegram notification failed.");}}}
