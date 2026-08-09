using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Contracts.Signals;
using TradingSystem.Redis.Messaging.Contracts;
using TradingViewWebhookService.Configuration;
using TradingViewWebhookService.Models;

namespace TradingViewWebhookService.Services;

public sealed class TradingViewSignalPublisher(
    IRedisMessagePublisher publisher,
    IOptions<TradingViewWebhookOptions> options,
    ILogger<TradingViewSignalPublisher> logger)
{
    private readonly TradingViewWebhookOptions _options = options.Value;

    public async Task<PublishResult> PublishAsync(
        string botName,
        TradingViewSignalRequest request,
        CancellationToken ct)
    {
        if (!_options.Enabled)
            return PublishResult.Rejected(503, "TradingView webhook ingress is disabled.");

        if (!SecretEquals(request.Secret, _options.Secret))
            return PublishResult.Rejected(403, "Unauthorized.");

        botName = botName.Trim().ToUpperInvariant();

        if (!_options.Bots.TryGetValue(botName, out var bot) || !bot.Enabled)
            return PublishResult.Rejected(404, $"TradingView signals are not enabled for bot '{botName}'.");

        var action = request.Action?.Trim().ToUpperInvariant();
        if (action is not ("LONG" or "SHORT"))
            return PublishResult.Rejected(400, "Action must be LONG or SHORT.");

        var configuredSymbol = bot.Symbol.Trim().ToUpperInvariant();
        var symbol = string.IsNullOrWhiteSpace(request.Symbol)
            ? configuredSymbol
            : request.Symbol.Trim().ToUpperInvariant();

        if (!symbol.Equals(configuredSymbol, StringComparison.OrdinalIgnoreCase))
            return PublishResult.Rejected(
                400,
                $"Symbol '{symbol}' is not allowed for {botName}. Expected '{configuredSymbol}'.");

        var signalId = string.IsNullOrWhiteSpace(request.SignalId)
            ? Guid.NewGuid().ToString()
            : request.SignalId.Trim();

        if (!Guid.TryParse(signalId, out _))
            return PublishResult.Rejected(400, "signal_id must be a valid GUID when supplied.");

        var generatedAtUtc = NormalizeUtc(request.GeneratedAtUtc);

        var message = new TradingSignalMessage(
            signalId,
            botName,
            symbol,
            action,
            "TRADINGVIEW",
            generatedAtUtc);

        await publisher.PublishAsync(
            RedisChannels.StrategySignals,
            message,
            ct);

        logger.LogInformation(
            "TradingView signal published. SignalId = {SignalId}, Bot = {Bot}, Symbol = {Symbol}, Action = {Action}",
            signalId,
            botName,
            symbol,
            action);

        return PublishResult.Accepted(
            new TradingViewSignalResponse(
                "received",
                signalId,
                botName,
                symbol,
                action,
                "TRADINGVIEW",
                generatedAtUtc));
    }

    private static DateTime NormalizeUtc(DateTime? value)
    {
        if (value is null)
            return DateTime.UtcNow;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }

    private static bool SecretEquals(string? supplied, string expected)
    {
        if (string.IsNullOrWhiteSpace(supplied) ||
            string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        var left = Encoding.UTF8.GetBytes(supplied);
        var right = Encoding.UTF8.GetBytes(expected);

        return left.Length == right.Length &&
               CryptographicOperations.FixedTimeEquals(left, right);
    }
}

public sealed record PublishResult(
    bool Succeeded,
    int StatusCode,
    string? Error,
    TradingViewSignalResponse? Response)
{
    public static PublishResult Rejected(int statusCode, string error)
        => new(false, statusCode, error, null);

    public static PublishResult Accepted(TradingViewSignalResponse response)
        => new(true, StatusCodes.Status202Accepted, null, response);
}
