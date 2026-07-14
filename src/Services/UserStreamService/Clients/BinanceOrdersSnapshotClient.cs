using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UserStreamService.Configuration;

namespace UserStreamService.Clients;

public sealed class BinanceOrdersSnapshotClient(
    HttpClient httpClient,
    IOptions<BinanceUserStreamOptions> options,
    ILogger<BinanceOrdersSnapshotClient> logger)
{
    private readonly BinanceUserStreamOptions _options = options.Value;

    public Task<JsonElement[]> GetOpenNormalOrdersAsync(string symbol, CancellationToken cancellationToken)
        => SendSignedArrayAsync("/fapi/v1/openOrders", symbol, cancellationToken);

    public Task<JsonElement[]> GetOpenAlgoOrdersAsync(string symbol, CancellationToken cancellationToken)
        => SendSignedArrayAsync("/fapi/v1/algo/openOrders", symbol, cancellationToken);

    private async Task<JsonElement[]> SendSignedArrayAsync(
        string path,
        string symbol,
        CancellationToken cancellationToken)
    {
        ValidateCredentials();

        var query = string.Join('&',
            $"symbol={Uri.EscapeDataString(symbol)}",
            $"recvWindow={_options.ReceiveWindow.ToString(CultureInfo.InvariantCulture)}",
            $"timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)}");

        var signature = Sign(query, _options.ApiSecret);
        var url = $"{_options.BaseUrl.TrimEnd('/')}{path}?{query}&signature={signature}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-MBX-APIKEY", _options.ApiKey);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Failed to fetch Binance orders snapshot. Path={Path}, Status={Status}, Body={Body}",
                path,
                (int)response.StatusCode,
                body);
            return [];
        }

        using var document = JsonDocument.Parse(body);
        return document.RootElement.EnumerateArray().Select(static item => item.Clone()).ToArray();
    }

    private void ValidateCredentials()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.ApiSecret))
            throw new InvalidOperationException("Binance API credentials are missing.");
    }

    private static string Sign(string value, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}
