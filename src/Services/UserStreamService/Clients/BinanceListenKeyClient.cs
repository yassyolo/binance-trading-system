using System.Text.Json;

namespace UserStreamService.Clients;

public sealed class BinanceListenKeyClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<BinanceListenKeyClient> logger)
{
    public async Task<string> CreateListenKeyAsync(CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();
        var baseUrl = GetBaseUrl();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/fapi/v1/listenKey");

        request.Headers.Add("X-MBX-APIKEY", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to create listenKey. Status={response.StatusCode}, Body={responseBody}");

        using var document = JsonDocument.Parse(responseBody);

        var listenKey = document.RootElement.GetProperty("listenKey").GetString();

        if (string.IsNullOrWhiteSpace(listenKey))
            throw new InvalidOperationException("Binance returned empty listenKey.");

        logger.LogInformation("ListenKey created: {Prefix}...", listenKey[..Math.Min(12, listenKey.Length)]);

        return listenKey;
    }

    public async Task KeepAliveAsync(string listenKey, CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();
        var baseUrl = GetBaseUrl();

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{baseUrl}/fapi/v1/listenKey?listenKey={Uri.EscapeDataString(listenKey)}");

        request.Headers.Add("X-MBX-APIKEY", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"ListenKey keepalive failed. Status={response.StatusCode}, Body={responseBody}");

        logger.LogDebug("ListenKey keepalive sent.");
    }

    private string GetApiKey()
    {
        var apiKey = configuration["Binance:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Binance API key is missing.");

        return apiKey;
    }

    private string GetBaseUrl()
        => configuration["Binance:BaseUrl"] ?? "https://fapi.binance.com";
}