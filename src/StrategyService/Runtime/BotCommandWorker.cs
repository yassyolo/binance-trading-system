using Microsoft.Extensions.Options;
using StrategyService.Runtime.Configuration;
using System.Text.Json;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.BotRuntime.Commands;
using TradingSystem.BotRuntime.Commands.Models;
using TradingSystem.BotRuntime.Commands.Models.Enums;
using TradingSystem.BotRuntime.Runtime.Contracts;
using TradingSystem.BotRuntime.Runtime.Models.Enums;
using TradingSystem.Operations.Contracts;
using TradingSystem.Operations.Models;

namespace StrategyService.Runtime;

public sealed class BotCommandWorker(
    IBotCommandQueue commandQueue,
    IBotRuntimeStateStore stateStore,
    IBotRuntimeStateProvider stateProvider,
    ITradeExecutor tradeExecutor,
    IPositionStore positionStore,
    LivePositionLifecycleRecorder lifecycle,
    IAuditLog auditLog,
    IOptions<BotRuntimeOptions> options,
    ILogger<BotCommandWorker> logger)
    : BackgroundService
{
    private readonly BotRuntimeOptions _options = options.Value;
    private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Bot command worker is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _options.CommandPollSeconds)));

        do
        {
            try
            {
                await ProcessBatchAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Bot command batch failed. Worker = {WorkerId}", _workerId);
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        var commands = await commandQueue.ClaimPendingAsync(_workerId, _options.CommandBatchSize, TimeSpan.FromSeconds(Math.Max(10, _options.CommandProcessingTimeoutSeconds)), ct);

        foreach (var command in commands)
        {
            try
            {
                await ProcessAsync(command, ct);
                
                await commandQueue.CompleteAsync(command.CommandId, _workerId, ct);
                
                await TryAuditAsync(command, "Completed", null);

                logger.LogInformation("Bot command completed. CommandId = {CommandId} Bot = {Bot} Command = {Command}", command.CommandId, command.BotName, command.Command);
            }
            catch (UnsupportedBotCommandException exception)
            {
                await commandQueue.RejectAsync(command.CommandId, _workerId, exception.Message, ct);
                
                await TryAuditAsync(command, "Rejected", exception.Message);

                logger.LogWarning("Bot command rejected. CommandId = {CommandId} Reason = {Reason}", command.CommandId, exception.Message);
            }
            catch (Exception ex)
            {
                var retryable = !IsPermanentFailure(ex) && command.AttemptCount < _options.MaximumCommandAttempts;

                await commandQueue.FailAsync(command.CommandId, _workerId, ex.Message, retryable, ct);

                if (!retryable)
                    await TryAuditAsync(command, "Failed", ex.Message);

                logger.LogError(ex, "Bot command failed. CommandId = {CommandId} Retryable = {Retryable}", command.CommandId, retryable);
            }
        }
    }

    private async Task ProcessAsync(BotCommand command, CancellationToken ct)
    {
        switch (command.Command)
        {
            case BotCommandType.ClosePosition:
                await ProcessClosePositionAsync(command, ct);
                return;
            case BotCommandType.CancelTakeProfit:
            case BotCommandType.RecreateTakeProfit:
                throw new UnsupportedBotCommandException($"Command '{command.Command}' requires a protected order-command handler which is not implemented yet.");
        }

        await ProcessRuntimeStateCommandAsync(command, ct);
    }

    private async Task ProcessRuntimeStateCommandAsync(BotCommand command, CancellationToken ct)
    {
        var current = await stateProvider.GetRequiredAsync(command.BotName, ct);

        var target = command.Command switch
        {
            BotCommandType.Start => BotRuntimeStatus.Running,
            BotCommandType.Resume => BotRuntimeStatus.Running,
            BotCommandType.Pause => BotRuntimeStatus.Paused,
            BotCommandType.Stop => BotRuntimeStatus.Stopped,
            BotCommandType.EmergencyStop => BotRuntimeStatus.EmergencyStopped,
            _ => throw new UnsupportedBotCommandException($"Unsupported command '{command.Command}'.")
        };

        if (current.Status != target)
        {
            await stateStore.TransitionAsync(
                command.BotName,
                target,
                current.Version,
                command.RequestedBy,
                command.Reason,
                target == BotRuntimeStatus.Running,
                ct);

            stateProvider.Invalidate(command.BotName);
        }
        else
        {
            logger.LogInformation("Bot runtime command requires no state transition. Bot = {Bot} Status = {Status}", command.BotName, current.Status);
        }

        if (command.Command is BotCommandType.Stop or BotCommandType.EmergencyStop)
            await ProcessStopSideEffectsAsync(command, ct);
    }

    private async Task ProcessStopSideEffectsAsync(BotCommand command, CancellationToken ct)
    {
        var root = command.Payload.RootElement;
        var cancelOpenOrders = ReadBoolean(root, "CancelOpenOrders", "cancelOpenOrders");
        var closeOpenPositions = ReadBoolean(root, "CloseOpenPositions", "closeOpenPositions");

        if (!cancelOpenOrders && !closeOpenPositions)
            return;

        if (cancelOpenOrders && !closeOpenPositions)
            throw new UnsupportedBotCommandException("CancelOpenOrders without CloseOpenPositions is not supported because it could leave an exposed p without protective orders.");

        var positions = await positionStore.GetAllAsync(command.BotName, ct);
        var activePositions = positions.Where(p => !p.Closed).ToArray();

        if (activePositions.Length == 0)
        {
            logger.LogInformation("Stop side effects require no positions close. Bot = {Bot} Command = {Command}", command.BotName, command.Command);

            return;
        }

        foreach (var position in activePositions)
        {
            ct.ThrowIfCancellationRequested();

            var reason = string.IsNullOrWhiteSpace(command.Reason)
                ? $"{command.Command}_CLOSE_OPEN_POSITION"
                : command.Reason.Trim();

            var result = await tradeExecutor.CloseAsync(command.BotName, position.ShortId, reason, ct);

            if (!result.Succeeded)
                throw new InvalidOperationException($"Runtime command '{command.Command}' changed the bot state but failed to close position '{position.ShortId}' for bot '{command.BotName}': {result.Reason}", result.Exception);

            await lifecycle.RecordClosedAsync(command.BotName, position.ShortId, reason, ct);

            logger.LogInformation("Runtime stop side effect closed position. CommandId = {CommandId} Bot = {Bot} Position = {Position} Command = {Command}", command.CommandId, command.BotName, position.ShortId, command.Command);
        }
    }

    private async Task ProcessClosePositionAsync(BotCommand command, CancellationToken ct)
    {
        var positionIdentifier = ReadRequiredPositionIdentifier(command);
        var reason = string.IsNullOrWhiteSpace(command.Reason)
            ? "MANUAL_POSITION_CLOSE"
            : command.Reason.Trim();

        var result = await tradeExecutor.CloseAsync(command.BotName, positionIdentifier, reason,  ct);

        if (!result.Succeeded)
        {
            var message = $"Position close failed for bot '{command.BotName}' and p '{positionIdentifier}': {result.Reason}";

            if (result.Reason.Contains("not found", StringComparison.OrdinalIgnoreCase))
                throw new PermanentBotCommandException(message, result.Exception);

            throw new InvalidOperationException(message, result.Exception);
        }

        await lifecycle.RecordClosedAsync(command.BotName, positionIdentifier, reason, ct);

        logger.LogInformation("Position close command executed. CommandId = {CommandId} Bot = {Bot} Position = {Position} Result = {Reason}", command.CommandId, command.BotName, positionIdentifier, result.Reason);
    }

    private static string ReadRequiredPositionIdentifier(BotCommand command)
    {
        var root = command.Payload.RootElement;

        if (TryReadString(root, "PositionId", out var positionId))
            return positionId;
        if (TryReadString(root, "positionId", out positionId))
            return positionId;
        if (TryReadString(root, "ShortId", out positionId))
            return positionId;
        if (TryReadString(root, "shortId", out positionId))
            return positionId;

        throw new UnsupportedBotCommandException($"Command '{command.Command}' requires PositionId in its payload.");
    }

    private static bool ReadBoolean(JsonElement root, string pascalCase, string camelCase)
    {
        if (TryReadBoolean(root, pascalCase, out var value))
            return value;
        if (TryReadBoolean(root, camelCase, out value))
            return value;
        return false;
    }

    private static bool TryReadBoolean(JsonElement root, string propertyName, out bool value)
    {
        value = false;

        if (!root.TryGetProperty(propertyName, out var property))
            return false;

        if (property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            return false;

        value = property.GetBoolean();
        return true;
    }

    private static bool TryReadString(JsonElement root, string propertyName, out string value)
    {
        value = string.Empty;

        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var parsedValue = property.GetString();
        if (string.IsNullOrWhiteSpace(parsedValue))
            return false;

        value = parsedValue.Trim();
        return true;
    }

    private static bool IsPermanentFailure(Exception ex)
    {
        if (ex is PermanentBotCommandException)
            return true;

        return ex is InvalidOperationException && ex.Message.StartsWith("Runtime state was not found for bot",StringComparison.OrdinalIgnoreCase);
    }

    private async Task TryAuditAsync(BotCommand command, string status, string? error)
    {
        try
        {
            await auditLog.WriteAsync(
                new AuditEvent(
                    AuditId: Guid.NewGuid(),
                    OccurredAtUtc: DateTime.UtcNow,
                    Actor: command.RequestedBy,
                    Action: $"BotCommand.{command.Command}.{status}",
                    EntityType: "BotCommand",
                    EntityId: command.CommandId.ToString(),
                    Reason: command.Reason,
                    CorrelationId: command.CommandId.ToString(),
                    IpAddress: null,
                    OldValueJson: null,
                    NewValueJson: null,
                    Metadata: new Dictionary<string, string>
                    {
                        ["botName"] = command.BotName,
                        ["command"] = command.Command.ToString(),
                        ["status"] = status,
                        ["attemptCount"] = command.AttemptCount.ToString(),
                        ["workerId"] = _workerId,
                        ["error"] = error ?? string.Empty
                    }),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Bot command audit persistence failed. CommandId = {CommandId}", command.CommandId);
        }
    }

    private sealed class PermanentBotCommandException(
        string message,
        Exception? innerException = null)
        : Exception(message, innerException);

    private sealed class UnsupportedBotCommandException(string message)
        : Exception(message);
}
