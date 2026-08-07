using System.Text.Json;
using TradingSystem.BotRuntime.Commands.Models.Enums;

namespace TradingSystem.BotRuntime.Commands.Models;

public sealed record BotCommand(
    Guid CommandId,
    string BotName,
    BotCommandType Command,
    BotCommandStatus Status,
    string RequestedBy,
    string Reason,
    JsonDocument Payload,
    DateTime RequestedAtUtc,
    int AttemptCount);
