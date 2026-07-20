using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TradingSystem.Binance.UserStream;

public sealed class BinanceListenKeyClient(HttpClient http, IOptions<BinanceUserStreamOptions> options):IBinanceListenKeyClient
{
    private readonly BinanceUserStreamOptions _o = options.Value;
    public async Task<string>CreateAsync(CancellationToken ct){using var r = Req(HttpMethod.Post, "/fapi/v1/listenKey");using var res = await http.SendAsync(r, ct);var body = await res.Content.ReadAsStringAsync(ct);Ensure(res, body, "create listen key");using var d = JsonDocument.Parse(body);var key = d.RootElement.GetProperty("listenKey").GetString();return !string.IsNullOrWhiteSpace(key)?key:throw new InvalidOperationException("Binance returned an empty listen key.");}
    public async Task KeepAliveAsync(string key, CancellationToken ct){using var r = Req(HttpMethod.Put, $"/fapi/v1/listenKey?listenKey = {Uri.EscapeDataString(key)}");using var res = await http.SendAsync(r, ct);var body = await res.Content.ReadAsStringAsync(ct);Ensure(res, body, "keep listen key alive");}
    HttpRequestMessage Req(HttpMethod method, string path){if(string.IsNullOrWhiteSpace(_o.ApiKey))throw new InvalidOperationException("Binance user-stream API key is missing.");var r = new HttpRequestMessage(method, $"{_o.RestBaseUrl.TrimEnd('/')}{path}");r.Headers.Add("X-MBX-APIKEY", _o.ApiKey);return r;}
    static void Ensure(HttpResponseMessage r, string body, string op){if(!r.IsSuccessStatusCode)throw new BinanceApiException(r.StatusCode, body, $"Failed to {op}.");}
}
