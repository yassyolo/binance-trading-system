using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingSystem.Binance.Exceptions;
using TradingSystem.Binance.UserStream.Configuration;
using TradingSystem.Binance.UserStream.Contracts;

namespace TradingSystem.Binance.UserStream;

public sealed class BinanceListenKeyClient(
    HttpClient http,
    IOptions<BinanceUserStreamOptions> options) 
    : IBinanceListenKeyClient
{
    private readonly BinanceUserStreamOptions _options = options.Value;

    public async Task<string> CreateAsync(CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Post, "fapi/v1/listenKey");
        using var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response, body, "create listen key");

        using var document = JsonDocument.Parse(body);
        var key = document.RootElement.GetProperty("listenKey").GetString();
        return !string.IsNullOrWhiteSpace(key)
            ? key
            : throw new InvalidOperationException("Binance returned an empty listen key.");
    }

    public async Task KeepAliveAsync(string key, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        using var request = CreateRequest(HttpMethod.Put, $"fapi/v1/listenKey?listenKey={Uri.EscapeDataString(key)}");
        using var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response, body, "keep listen key alive");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Binance user-stream API key is missing.");

        var baseUrl = _options.RestBaseUrl.TrimEnd('/');
        var request = new HttpRequestMessage(method, $"{baseUrl}/{relativePath.TrimStart('/')}");
        request.Headers.Add("X-MBX-APIKEY", _options.ApiKey);
        return request;
    }

    private static void EnsureSuccess(HttpResponseMessage response, string body, string operation)
    {
        if (!response.IsSuccessStatusCode)
            throw new BinanceApiException(response.StatusCode, body, operation);
    }
}
