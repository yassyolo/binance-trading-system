namespace StrategyService.Bots.Bot8011.Models;

public sealed record CreatedStop3Order(
    string AlgoOrderId,
    string ClientAlgoId, 
    string? Status,
    decimal TriggerPrice);
