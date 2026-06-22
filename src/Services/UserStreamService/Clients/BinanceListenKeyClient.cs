using System.Text.Json;

namespace UserStreamService.Clients;

public sealed class BinanceListenKeyClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BinanceListenKeyClient> _logger;

    public BinanceListenKeyClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BinanceListenKeyClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> CreateListenKeyAsync(CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();
        var baseUrl = _configuration["Binance:BaseUrl"] ?? "https://fapi.binance.com";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/fapi/v1/listenKey");

        request.Headers.Add("X-MBX-APIKEY", apiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(json);

        var listenKey = document.RootElement
            .GetProperty("listenKey")
            .GetString();

        if (string.IsNullOrWhiteSpace(listenKey))
            throw new InvalidOperationException("Binance returned empty listenKey.");

        _logger.LogInformation("ListenKey created: {Prefix}...", listenKey[..Math.Min(12, listenKey.Length)]);

        return listenKey;
    }

    public async Task KeepAliveAsync(string listenKey, CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();
        var baseUrl = _configuration["Binance:BaseUrl"] ?? "https://fapi.binance.com";

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{baseUrl}/fapi/v1/listenKey?listenKey={Uri.EscapeDataString(listenKey)}");

        request.Headers.Add("X-MBX-APIKEY", apiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogDebug("ListenKey keepalive sent.");
    }

    private string GetApiKey()
    {
        var apiKey = _configuration["Binance:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Binance API key is missing.");

        return apiKey;
    }
}