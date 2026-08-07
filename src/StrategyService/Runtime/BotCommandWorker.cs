using Microsoft.Extensions.Options;
using System.Text.Json;
using TradingSystem.Application.Execution;
using TradingSystem.BotRuntime.Commands;
using TradingSystem.BotRuntime.Runtime;
using TradingSystem.Operations;

namespace StrategyService.Runtime;

public sealed class BotCommandWorker(
    IBotCommandQueue queue,
    IBotRuntimeStateStore stateStore,
    IBotRuntimeStateProvider stateProvider,
    ITradeExecutor tradeExecutor,
    IAuditLog auditLog,
    IOptions<BotRuntimeOptions> options,
    ILogger<BotCommandWorker> logger)
    : BackgroundService
{
    private readonly BotRuntimeOptions _options = options.Value;

    private readonly string _workerId =
        $"{Environment.MachineName}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation(
                "Bot command worker is disabled.");

            return;
        }

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(
                Math.Max(1, _options.CommandPollSeconds)));

        do
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Bot command batch failed. Worker = {WorkerId}",
                    _workerId);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(
        CancellationToken ct)
    {
        var commands = await queue.ClaimPendingAsync(
            _workerId,
            _options.CommandBatchSize,
            TimeSpan.FromSeconds(
                Math.Max(
                    10,
                    _options.CommandProcessingTimeoutSeconds)),
            ct);

        foreach (var command in commands)
        {
            try
            {
                await ProcessAsync(
                    command,
                    ct);

                await queue.CompleteAsync(
                    command.CommandId,
                    _workerId,
                    ct);

                await TryAuditAsync(command, "Completed", null);

                logger.LogInformation(
                    "Bot command completed. CommandId = {CommandId} Bot = {Bot} Command = {Command}",
                    command.CommandId,
                    command.BotName,
                    command.Command);
            }
            catch (UnsupportedBotCommandException exception)
            {
                await queue.RejectAsync(
                    command.CommandId,
                    _workerId,
                    exception.Message,
                    ct);

                await TryAuditAsync(command, "Rejected", exception.Message);

                logger.LogWarning(
                    "Bot command rejected. CommandId = {CommandId} Reason = {Reason}",
                    command.CommandId,
                    exception.Message);
            }
            catch (Exception exception)
            {
                var retryable =
                    !IsPermanentFailure(exception) &&
                    command.AttemptCount <
                    _options.MaximumCommandAttempts;

                await queue.FailAsync(
                    command.CommandId,
                    _workerId,
                    exception.Message,
                    retryable,
                    ct);

                if (!retryable)
                    await TryAuditAsync(command, "Failed", exception.Message);

                logger.LogError(
                    exception,
                    "Bot command failed. CommandId = {CommandId} Retryable = {Retryable}",
                    command.CommandId,
                    retryable);
            }
        }
    }

    private async Task ProcessAsync(
        BotCommand command,
        CancellationToken ct)
    {
        switch (command.Command)
        {
            case BotCommandType.ClosePosition:
                await ProcessClosePositionAsync(
                    command,
                    ct);

                return;

            case BotCommandType.CancelTakeProfit:
            case BotCommandType.RecreateTakeProfit:
                throw new UnsupportedBotCommandException(
                    $"Command '{command.Command}' requires a protected order-command handler which is not implemented yet.");
        }

        await ProcessRuntimeStateCommandAsync(
            command,
            ct);
    }

    private async Task ProcessRuntimeStateCommandAsync(
        BotCommand command,
        CancellationToken ct)
    {
        var current =
            await stateProvider.GetRequiredAsync(
                command.BotName,
                ct);

        var target = command.Command switch
        {
            BotCommandType.Start =>
                BotRuntimeStatus.Running,

            BotCommandType.Resume =>
                BotRuntimeStatus.Running,

            BotCommandType.Pause =>
                BotRuntimeStatus.Paused,

            BotCommandType.Stop =>
                BotRuntimeStatus.Stopped,

            BotCommandType.EmergencyStop =>
                BotRuntimeStatus.EmergencyStopped,

            _ => throw new UnsupportedBotCommandException(
                $"Unsupported command '{command.Command}'.")
        };

        if (current.Status == target)
        {
            logger.LogInformation(
                "Bot runtime command requires no transition. Bot = {Bot} Status = {Status}",
                command.BotName,
                current.Status);

            return;
        }

        var executionEnabled =
            target == BotRuntimeStatus.Running;

        await stateStore.TransitionAsync(
            command.BotName,
            target,
            current.Version,
            command.RequestedBy,
            command.Reason,
            executionEnabled,
            ct);

        stateProvider.Invalidate(
            command.BotName);
    }

    private async Task ProcessClosePositionAsync(
        BotCommand command,
        CancellationToken ct)
    {
        var positionIdentifier =
            ReadRequiredPositionIdentifier(command);

        var reason =
            string.IsNullOrWhiteSpace(command.Reason)
                ? "MANUAL_POSITION_CLOSE"
                : command.Reason.Trim();

        var result = await tradeExecutor.CloseAsync(
            command.BotName,
            positionIdentifier,
            reason,
            ct);

        if (!result.Succeeded)
        {
            var message =
                $"Position close failed for bot '{command.BotName}' " +
                $"and position '{positionIdentifier}': {result.Reason}";

            if (result.Reason.Contains("not found", StringComparison.OrdinalIgnoreCase))
                throw new PermanentBotCommandException(message, result.Exception);

            throw new InvalidOperationException(message, result.Exception);
        }

        logger.LogInformation(
            "Position close command executed. CommandId = {CommandId} Bot = {Bot} Position = {Position} Result = {Reason}",
            command.CommandId,
            command.BotName,
            positionIdentifier,
            result.Reason);
    }

    private static string ReadRequiredPositionIdentifier(
        BotCommand command)
    {
        var root = command.Payload.RootElement;

        if (TryReadString(
                root,
                "PositionId",
                out var positionId))
        {
            return positionId;
        }

        if (TryReadString(
                root,
                "positionId",
                out positionId))
        {
            return positionId;
        }

        if (TryReadString(
                root,
                "ShortId",
                out positionId))
        {
            return positionId;
        }

        if (TryReadString(
                root,
                "shortId",
                out positionId))
        {
            return positionId;
        }

        throw new UnsupportedBotCommandException(
            $"Command '{command.Command}' requires PositionId in its payload.");
    }

    private static bool TryReadString(
        JsonElement root,
        string propertyName,
        out string value)
    {
        value = string.Empty;

        if (!root.TryGetProperty(
                propertyName,
                out var property))
        {
            return false;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var parsedValue = property.GetString();

        if (string.IsNullOrWhiteSpace(parsedValue))
        {
            return false;
        }

        value = parsedValue.Trim();

        return true;
    }

    private static bool IsPermanentFailure(Exception exception)
    {
        if (exception is PermanentBotCommandException)
            return true;

        return exception is InvalidOperationException &&
               exception.Message.StartsWith(
                   "Runtime state was not found for bot",
                   StringComparison.OrdinalIgnoreCase);
    }

    private async Task TryAuditAsync(
        BotCommand command,
        string status,
        string? error)
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
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Bot command audit persistence failed. CommandId = {CommandId}",
                command.CommandId);
        }
    }

    private sealed class PermanentBotCommandException(
        string message,
        Exception? innerException = null)
        : Exception(message, innerException);

    private sealed class UnsupportedBotCommandException(
        string message)
        : Exception(message);
}