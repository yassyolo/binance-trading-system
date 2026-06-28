using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingSystem.Binance.Configuration;

namespace TradingSystem.Binance.Orders;

public sealed class BinanceFuturesOrderClient : IBinanceFuturesOrderClient
{
    private readonly HttpClient _httpClient;
    private readonly BinanceFuturesOptions _options;

    public BinanceFuturesOrderClient(
        HttpClient httpClient,
        IOptions<BinanceFuturesOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Remove("X-MBX-APIKEY");
        _httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _options.ApiKey);
    }

    public async Task<BinanceOrderResult> PlaceMarketOrderAsync(
        string symbol,
        string side,
        string positionSide,
        decimal quantity,
        string clientOrderId,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = symbol,
            ["side"] = side,
            ["positionSide"] = positionSide,
            ["type"] = "MARKET",
            ["quantity"] = ToBinanceDecimal(quantity),
            ["newClientOrderId"] = clientOrderId,
            ["newOrderRespType"] = "RESULT"
        };

        var json = await SendSignedAsync(
            HttpMethod.Post,
            "/fapi/v1/order",
            parameters,
            cancellationToken);

        return ParseOrderResult(json);
    }

    public async Task<BinanceAlgoOrderResult> PlaceTakeProfitMarketAlgoOrderAsync(
        string symbol,
        string side,
        string positionSide,
        decimal quantity,
        decimal stopPrice,
        string clientOrderId,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = symbol,
            ["side"] = side,
            ["positionSide"] = positionSide,
            ["algoType"] = "CONDITIONAL",
            ["type"] = "TAKE_PROFIT_MARKET",
            ["quantity"] = ToBinanceDecimal(quantity),
            ["triggerPrice"] = ToBinanceDecimal(stopPrice),
            ["workingType"] = "MARK_PRICE",
            ["clientAlgoId"] = clientOrderId
        };

        var json = await SendSignedAsync(
            HttpMethod.Post,
            "/fapi/v1/algoOrder",
            parameters,
            cancellationToken);

        return ParseAlgoOrderResult(json);
    }

    public async Task<BinanceAlgoOrderResult> PlaceStopMarketAlgoOrderAsync(
        string symbol,
        string side,
        string positionSide,
        decimal quantity,
        decimal stopPrice,
        string clientOrderId,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = symbol,
            ["side"] = side,
            ["positionSide"] = positionSide,
            ["algoType"] = "CONDITIONAL",
            ["type"] = "STOP_MARKET",
            ["quantity"] = ToBinanceDecimal(quantity),
            ["triggerPrice"] = ToBinanceDecimal(stopPrice),
            ["workingType"] = "MARK_PRICE",
            ["clientAlgoId"] = clientOrderId
        };

        var json = await SendSignedAsync(
            HttpMethod.Post,
            "/fapi/v1/algoOrder",
            parameters,
            cancellationToken);

        return ParseAlgoOrderResult(json);
    }

    public Task CancelOrderAsync(
        string symbol,
        string orderId,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = symbol,
            ["orderId"] = orderId
        };

        return SendSignedAsync(
            HttpMethod.Delete,
            "/fapi/v1/order",
            parameters,
            cancellationToken);
    }

    public Task CancelAlgoOrderAsync(
        string symbol,
        string algoOrderId,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = symbol,
            ["algoId"] = algoOrderId
        };

        return SendSignedAsync(
            HttpMethod.Delete,
            "/fapi/v1/algoOrder",
            parameters,
            cancellationToken);
    }

    private async Task<string> SendSignedAsync(
        HttpMethod method,
        string endpoint,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        parameters["recvWindow"] = _options.ReceiveWindow.ToString(CultureInfo.InvariantCulture);
        parameters["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);

        var query = BuildQueryString(parameters);
        var signature = Sign(query);
        var signedQuery = $"{query}&signature={signature}";

        using var request = new HttpRequestMessage(method, $"{endpoint}?{signedQuery}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Binance request failed. Status={(int)response.StatusCode}, Body={content}");

        return content;
    }

    private string Sign(string query)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_options.SecretKey);
        var queryBytes = Encoding.UTF8.GetBytes(query);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(queryBytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BuildQueryString(Dictionary<string, string> parameters)
        => string.Join("&", parameters.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));

    private static string ToBinanceDecimal(decimal value)
        => value.ToString("0.########", CultureInfo.InvariantCulture);

    private static BinanceOrderResult ParseOrderResult(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new BinanceOrderResult
        {
            Symbol = GetString(root, "symbol"),
            ClientOrderId = GetString(root, "clientOrderId"),
            OrderId = GetFlexibleString(root, "orderId"),
            Status = TryGetString(root, "status"),
            AveragePrice = TryGetDecimal(root, "avgPrice")
        };
    }

    private static BinanceAlgoOrderResult ParseAlgoOrderResult(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new BinanceAlgoOrderResult
        {
            Symbol = GetString(root, "symbol"),
            ClientOrderId = GetString(root, "clientAlgoId"),
            AlgoOrderId = GetFlexibleString(root, "algoId"),
            Status = TryGetString(root, "status")
        };
    }

    private static string GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value)
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string GetFlexibleString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
            return string.Empty;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            _ => value.ToString()
        };
    }

    private static string? TryGetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value)
            ? value.GetString()
            : null;

    private static decimal? TryGetDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            return number;

        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return null;
    }

    public async Task<BinanceOrderResult> PlaceLimitOrderAsync(
    string symbol,
    string side,
    string positionSide,
    decimal quantity,
    decimal price,
    string clientOrderId,
    CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = symbol,
            ["side"] = side,
            ["positionSide"] = positionSide,
            ["type"] = "LIMIT",
            ["timeInForce"] = "GTC",
            ["quantity"] = ToBinanceDecimal(quantity),
            ["price"] = ToBinanceDecimal(price),
            ["newClientOrderId"] = clientOrderId,
            ["newOrderRespType"] = "RESULT"
        };

        var json = await SendSignedAsync(
            HttpMethod.Post,
            "/fapi/v1/order",
            parameters,
            cancellationToken);

        return ParseOrderResult(json);
    }
}