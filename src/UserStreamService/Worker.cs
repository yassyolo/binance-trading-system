using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Binance.UserStream;
using UserStreamService.Configuration;
using UserStreamService.Services;

namespace UserStreamService;

public sealed class Worker(IBinanceListenKeyClient listenKeys, IBinanceUserStreamClient stream, UserStreamEventProcessor processor, HealingPublisher healing, IOptions<BinanceUserStreamOptions> binance, IOptions<UserStreamServiceOptions> service, TimeProvider time, ILogger<Worker> logger):BackgroundService
{
    private readonly object _sync = new();private string? _listenKey;private DateTimeOffset? _disconnectedAt;private readonly BinanceUserStreamOptions _b = binance.Value;private readonly UserStreamServiceOptions _s = service.Value;
    protected override Task ExecuteAsync(CancellationToken ct) => Task.WhenAll(StreamLoop(ct), KeepAliveLoop(ct));
    async Task StreamLoop(CancellationToken ct){while(!ct.IsCancellationRequested){try{var key = await listenKeys.CreateAsync(ct);lock(_sync)_listenKey = key;await stream.RunAsync(key, processor.ProcessAsync, OnConnected, ct);}catch(OperationCanceledException)when(ct.IsCancellationRequested){break;}catch(Exception ex){logger.LogError(ex, "Binance user stream failed.");}finally{lock(_sync){_listenKey = null;_disconnectedAt?? = time.GetUtcNow();}}if(!ct.IsCancellationRequested)await Task.Delay(TimeSpan.FromSeconds(_s.ReconnectDelaySeconds), ct);}}
    async Task KeepAliveLoop(CancellationToken ct){using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_b.ListenKeyKeepAliveSeconds));while(await timer.WaitForNextTickAsync(ct)){string? key;lock(_sync)key = _listenKey;if(string.IsNullOrWhiteSpace(key))continue;try{await listenKeys.KeepAliveAsync(key, ct);}catch(Exception ex){logger.LogWarning(ex, "Listen-key keepalive failed.");}}}
    async Task OnConnected(CancellationToken ct){DateTimeOffset? at;lock(_sync){at = _disconnectedAt;_disconnectedAt = null;}if(at is not null)await healing.PublishAfterReconnectAsync(time.GetUtcNow()-at.Value, ct);}
}
