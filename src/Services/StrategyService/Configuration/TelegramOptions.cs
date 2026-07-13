using System.Text;
using Microsoft.Extensions.Options;

namespace StrategyService.Configuration;

public sealed record TelegramOptions
{
    public const string SectionName = "Telegram";
    public bool Enabled { get; init; }
    public string BotToken { get; init; } = string.Empty;
    public string ChatId { get; init; } = string.Empty;
}