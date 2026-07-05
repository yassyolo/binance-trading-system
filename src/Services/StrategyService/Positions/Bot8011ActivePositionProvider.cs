using StrategyService.Services;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;

namespace StrategyService.Positions;

public sealed class Bot8011ActivePositionProvider : IBotActivePositionProvider
{
    private readonly RedisActivePositionProvider _redisProvider;

    public Bot8011ActivePositionProvider(RedisActivePositionProvider redisProvider)
    {
        _redisProvider = redisProvider;
    }

    public string BotName => "BOT8011";

    public Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        return _redisProvider.GetActivePositionsAsync(
            BotName,
            symbol,
            cancellationToken);
    }
}