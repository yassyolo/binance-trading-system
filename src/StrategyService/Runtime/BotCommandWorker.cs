using Microsoft.Extensions.Options;
using TradingSystem.BotRuntime.Commands;
using TradingSystem.BotRuntime.Runtime;

namespace StrategyService.Runtime;

public sealed class BotCommandWorker(
    IBotCommandQueue queue, 
    IBotRuntimeStateStore stateStore, 
    IBotRuntimeStateProvider stateProvider, 
    IOptions<BotRuntimeOptions> options, 
    ILogger<BotCommandWorker> logger) : BackgroundService
{
    private readonly BotRuntimeOptions _options  =  options.Value;
    private readonly string _workerId  =  $"{Environment.MachineName}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Bot command worker is disabled.");
            return;
        }

        using var timer  =  new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1,  _options.CommandPollSeconds)));
        do
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,  "Bot command batch failed. Worker = {WorkerId}",  _workerId);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var commands  =  await queue.ClaimPendingAsync(
            _workerId, 
            _options.CommandBatchSize, 
            TimeSpan.FromSeconds(Math.Max(10,  _options.CommandProcessingTimeoutSeconds)), 
            cancellationToken);

        foreach (var command in commands)
        {
            try
            {
                await ProcessAsync(command,  cancellationToken);
                await queue.CompleteAsync(command.CommandId,  _workerId,  cancellationToken);
                logger.LogInformation("Bot command completed. CommandId = {CommandId} Bot = {Bot} Command = {Command}",  command.CommandId,  command.BotName,  command.Command);
            }
            catch (UnsupportedBotCommandException ex)
            {
                await queue.RejectAsync(command.CommandId,  _workerId,  ex.Message,  cancellationToken);
                logger.LogWarning("Bot command rejected. CommandId = {CommandId} Reason = {Reason}",  command.CommandId,  ex.Message);
            }
            catch (Exception ex)
            {
                var retryable  =  command.AttemptCount < _options.MaximumCommandAttempts;
                await queue.FailAsync(command.CommandId,  _workerId,  ex.Message,  retryable,  cancellationToken);
                logger.LogError(ex,  "Bot command failed. CommandId = {CommandId} Retryable = {Retryable}",  command.CommandId,  retryable);
            }
        }
    }

    private async Task ProcessAsync(BotCommand command,  CancellationToken cancellationToken)
    {
        var current  =  await stateProvider.GetRequiredAsync(command.BotName,  cancellationToken);
        var target  =  command.Command switch
        {
            BotCommandType.Start  =>  BotRuntimeStatus.Running, 
            BotCommandType.Resume  =>  BotRuntimeStatus.Running, 
            BotCommandType.Pause  =>  BotRuntimeStatus.Paused, 
            BotCommandType.Stop  =>  BotRuntimeStatus.Stopped, 
            BotCommandType.EmergencyStop  =>  BotRuntimeStatus.EmergencyStopped, 
            BotCommandType.ClosePosition or BotCommandType.CancelTakeProfit or BotCommandType.RecreateTakeProfit
                 =>  throw new UnsupportedBotCommandException($"Command '{command.Command}' requires the protected position-command handler and is not executed by the runtime-state worker."), 
            _  =>  throw new UnsupportedBotCommandException($"Unsupported command '{command.Command}'.")
        };

        if (current.Status == target)
            return;

        var executionEnabled  =  target == BotRuntimeStatus.Running;
        await stateStore.TransitionAsync(command.BotName,  target,  current.Version,  command.RequestedBy,  command.Reason,  executionEnabled,  cancellationToken);
        stateProvider.Invalidate(command.BotName);
    }

    private sealed class UnsupportedBotCommandException(string message) : Exception(message);
}
