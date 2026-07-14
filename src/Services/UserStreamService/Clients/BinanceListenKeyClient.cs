using System.Text.Json;
using Microsoft.Extensions.Options;
using UserStreamService.Configuration;

namespace UserStreamService.Clients;

public sealed class BinanceListenKeyClient(
    HttpClient httpClient,
    IOptions<BinanceUserStreamOptions> options,
    ILogger<BinanceListenKeyClient> logger)
{
    private readonly BinanceUserStreamOptions _options = options.Value;

    public async Task<string> CreateAsync(CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "/fapi/v1/listenKey");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        EnsureSuccess(response, body, "create listen key");

        using var document = JsonDocument.Parse(body);
        var listenKey = document.RootElement.TryGetProperty("listenKey", out var property)
            ? property.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(listenKey))
            throw new InvalidOperationException("Binance returned an empty listen key.");

        logger.LogInformation("Binance listen key created. Prefix={Prefix}", listenKey[..Math.Min(12, listenKey.Length)]);
        return listenKey;
    }

    public async Task KeepAliveAsync(string listenKey, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(
            HttpMethod.Put,
            $"/fapi/v1/listenKey?listenKey={Uri.EscapeDataString(listenKey)}");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, body, "keep listen key alive");

        logger.LogDebug("Binance listen key keepalive sent.");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Binance:ApiKey is missing.");

        var request = new HttpRequestMessage(method, $"{_options.BaseUrl.TrimEnd('/')}{path}");
        request.Headers.Add("X-MBX-APIKEY", _options.ApiKey);
        return request;
    }

    private static void EnsureSuccess(HttpResponseMessage response, string body, string operation)
    {
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to {operation}. Status={(int)response.StatusCode}, Body={body}");
    }
}
