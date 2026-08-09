using System.Globalization;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Domain.Signals;
using TradingSystem.PaperTrading.Models;

namespace StrategyService.Services;

public sealed class TelegramTradingEngineNotifier(
    TelegramNotificationService telegram)
    : ITradingEngineNotifier
{
    public Task DecisionMadeAsync(TradeSignal signal, decimal markPrice, StrategyDecision decision, CancellationToken ct)
        => Task.CompletedTask;

    public Task ExecutionCompletedAsync(TradeSignal signal, TradeExecutionResult result, CancellationToken ct)
    {
        if (!result.Succeeded)
        {
            return telegram.SendAsync(
                $"""
                ❌ EXECUTION FAILED

                Bot: {signal.BotName}
                Symbol: {signal.Symbol}
                Side: {signal.Side.ToString().ToUpperInvariant()}

                Reason:
                {NormalizeReason(result.Reason)}

                Position: {result.ShortId ?? "-"}
                Source: {NormalizeSource(signal.Source)}
                """,
                ct);
        }

        return telegram.SendAsync(
            $"""
            🟢 POSITION OPENED

            Bot: {signal.BotName}
            Symbol: {signal.Symbol}
            Side: {signal.Side.ToString().ToUpperInvariant()}

            {NormalizeReason(result.Reason)}

            Position: {result.ShortId ?? "-"}
            Source: {NormalizeSource(signal.Source)}
            """,
            ct);
    }

    public Task ProcessingFailedAsync(TradeSignal signal, Exception exception, CancellationToken ct)
        => telegram.SendAsync(
            $"""
            🚨 TRADING ERROR

            Bot: {signal.BotName}
            Symbol: {signal.Symbol}
            Side: {signal.Side.ToString().ToUpperInvariant()}

            {exception.Message}

            Signal: {signal.SignalId}
            Source: {NormalizeSource(signal.Source)}
            """,
            ct);

    public Task FinalOutcomeAsync(TradeSignal signal, string finalDecision, string? reason, CancellationToken ct)
    {
        if (finalDecision.Equals("Duplicate", StringComparison.OrdinalIgnoreCase) ||
            finalDecision.Equals("Open", StringComparison.OrdinalIgnoreCase) ||
            finalDecision.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        if (finalDecision.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return telegram.SendAsync(
                $"""
                ⚠️ SIGNAL REJECTED

                Bot: {signal.BotName}
                Symbol: {signal.Symbol}
                Side: {signal.Side.ToString().ToUpperInvariant()}

                Reason:
                {NormalizeReason(reason)}

                Signal: {signal.SignalId}
                Source: {NormalizeSource(signal.Source)}
                """,
                ct);
        }

        if (finalDecision.Equals("Block", StringComparison.OrdinalIgnoreCase))
        {
            if (TryParseRiskBlock(reason, out var rule, out var details))
            {
                return telegram.SendAsync(
                    $"""
                    🛡️ RISK BLOCKED

                    Bot: {signal.BotName}
                    Symbol: {signal.Symbol}
                    Side: {signal.Side.ToString().ToUpperInvariant()}
                    Rule: {rule}

                    Reason:
                    {details}

                    Signal: {signal.SignalId}
                    Source: {NormalizeSource(signal.Source)}
                    """,
                    ct);
            }

            return telegram.SendAsync(
                $"""
                ⛔ SIGNAL BLOCKED

                Bot: {signal.BotName}
                Symbol: {signal.Symbol}
                Side: {signal.Side.ToString().ToUpperInvariant()}

                Reason:
                {NormalizeReason(reason)}

                Signal: {signal.SignalId}
                Source: {NormalizeSource(signal.Source)}
                """,
                ct);
        }

        return Task.CompletedTask;
    }

    public Task PaperPositionClosedAsync(PaperTradingPosition position, decimal triggerPrice, string closeReason, CancellationToken ct)
    {
        var pnl = position.RealizedPnl;
        var icon = pnl switch { > 0 => "💰", < 0 => "🔴", _ => "⚪" };
        var exitPrice = position.ExitPrice ?? triggerPrice;
        var fees = position.EntryFee + (position.ExitFee ?? 0m);

        return telegram.SendAsync(
            $"""
            {icon} PAPER POSITION CLOSED

            Bot: {position.BotName}
            Symbol: {position.Symbol}
            Side: {position.Side.ToString().ToUpperInvariant()}

            Entry: {FormatDecimal(position.EntryPrice)}
            Exit: {FormatDecimal(exitPrice)}
            Quantity: {FormatDecimal(position.Quantity)}
            Realized PnL: {FormatSignedDecimal(pnl)}
            Fees: {FormatDecimal(fees)}

            Reason: {NormalizeReason(position.CloseReason ?? closeReason)}
            Position: {position.ShortId}
            Source: {NormalizeSource(position.Source)}
            """,
            ct);
    }

    private static bool TryParseRiskBlock(string? reason, out string rule, out string details)
    {
        rule = "UNKNOWN";
        details = NormalizeReason(reason);
        if (string.IsNullOrWhiteSpace(reason)) return false;
        const string prefix = "RISK_BLOCKED [";
        if (!reason.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        var end = reason.IndexOf(']', prefix.Length);
        if (end < 0) return false;
        rule = reason[prefix.Length..end].Trim();
        var remainder = reason[(end + 1)..].Trim();
        if (remainder.StartsWith(':')) remainder = remainder[1..].Trim();
        details = string.IsNullOrWhiteSpace(remainder) ? NormalizeReason(reason) : remainder;
        return true;
    }

    private static string NormalizeReason(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    private static string NormalizeSource(string? value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    private static string FormatDecimal(decimal value) => value.ToString("0.########", CultureInfo.InvariantCulture);
    private static string FormatSignedDecimal(decimal? value) => value is null ? "-" : value.Value >= 0 ? $"+{FormatDecimal(value.Value)}" : FormatDecimal(value.Value);
}