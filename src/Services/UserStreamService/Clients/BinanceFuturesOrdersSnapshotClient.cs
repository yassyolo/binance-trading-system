using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UserStreamService.Clients;

public sealed class BinanceFuturesOrdersSnapshotClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<BinanceFuturesOrdersSnapshotClient> logger)
{
    public Task<JsonElement[]> GetOpenNormalOrdersAsync(string symbol, CancellationToken cancellationToken)
        => SendSignedArrayRequestAsync(
            "/fapi/v1/openOrders",
            symbol,
            cancellationToken);

    public Task<JsonElement[]> GetOpenAlgoOrdersAsync(string symbol, CancellationToken cancellationToken)
        => SendSignedArrayRequestAsync(
            "/fapi/v1/algo/openOrders",
            symbol,
            cancellationToken);

    private async Task<JsonElement[]> SendSignedArrayRequestAsync(string path, string symbol, CancellationToken cancellationToken)
    {
        var apiKey = GetRequiredConfig("Binance:ApiKey");
        var secret = GetRequiredConfig("Binance:ApiSecret");
        var baseUrl = configuration["Binance:BaseUrl"] ?? "https://fapi.binance.com";

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var query = $"symbol={Uri.EscapeDataString(symbol)}&timestamp={timestamp}";
        var signature = Sign(query, secret);

        var url = $"{baseUrl}{path}?{query}&signature={signature}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-MBX-APIKEY", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to fetch Binance orders snapshot. Path={Path}, Status={Status}, Body={Body}", path, response.StatusCode, responseBody);

            return [];
        }

        using var document = JsonDocument.Parse(responseBody);

        return document.RootElement
            .EnumerateArray()
            .Select(x => x.Clone())
            .ToArray();
    }

    private static string Sign(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private string GetRequiredConfig(string key)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{key} is missing.");

        return value;
    }
}