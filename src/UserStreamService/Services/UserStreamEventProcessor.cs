using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Contracts.UserStream;
using TradingSystem.Redis.Messaging;
using UserStreamService.Configuration;

namespace UserStreamService.Services;

public sealed class UserStreamEventProcessor(IRedisMessagePublisher publisher, TimeProvider time, IOptions<UserStreamServiceOptions> options, ILogger<UserStreamEventProcessor> logger)
{
    private long _sequence;private readonly UserStreamServiceOptions _o = options.Value;
    public async Task ProcessAsync(string raw, CancellationToken ct)
    {
        try
        {
            using var d = JsonDocument.Parse(raw);
            var root = d.RootElement;
            
            if(!root.TryGetProperty("e", out var ep))
                return;
            
            var type = ep.GetString()??"unknown";
            var envelope = new UserStreamEnvelope
            {
                HubTimestampUtc = time.GetUtcNow().UtcDateTime, 
                HubSequence = Interlocked.Increment(ref _sequence), 
                Binance = root.Clone()
            };
            
            if(_o.PublishRaw)
                await publisher.PublishAsync(RedisChannels.UserStreamRaw, envelope, ct);
            
            switch(type)
            {
                case "ORDER_TRADE_UPDATE":
                
                case "ALGO_UPDATE":
                
                case "TRADE_LITE":
                    await publisher.PublishAsync(RedisChannels.UserStreamOrder, envelope, ct);
                    break;
                
                case "ACCOUNT_UPDATE":
                    await publisher.PublishAsync(RedisChannels.UserStreamAccount, envelope, ct);
                    break;
               
                case "listenKeyExpired":
                    logger.LogWarning("Binance listen key expired event received.");
                    break;
                
                default:logger.LogDebug("Ignored Binance user event {EventType}", type);
                    break;
            }
        }
        catch(JsonException ex)
        {
            logger.LogWarning(ex, "Invalid Binance user-stream JSON.");
        }
    }
}
