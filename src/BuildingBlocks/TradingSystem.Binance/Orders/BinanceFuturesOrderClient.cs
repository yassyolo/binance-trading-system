using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Positions;

namespace TradingSystem.Binance.Orders;

public sealed class BinanceFuturesOrderClient : IBinanceFuturesOrderClient
{
    private readonly HttpClient _httpClient;
    private readonly BinanceFuturesOptions options;

    public BinanceFuturesOrderClient(
        HttpClient httpClient,
        IOptions<BinanceFuturesOptions> _options)
    {
        _httpClient = httpClient;
        options = _options.Value;

        _httpClient.BaseAddress = new Uri(options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Remove("X-MBX-APIKEY");
        _httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", options.ApiKey);
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

    public Task CancelOrderAsync(string symbol, string orderId, CancellationToken cancellationToken)
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
        parameters["recvWindow"] = options.ReceiveWindow.ToString(CultureInfo.InvariantCulture);
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
        var keyBytes = Encoding.UTF8.GetBytes(options.SecretKey);
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
            AveragePrice = TryGetDecimal(root, "avgPrice"),
            ExecutedQuantity = TryGetDecimal(root, "executedQty"),
            CumulativeQuoteQuantity = TryGetDecimal(root, "cumQuote")
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
        => root.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;

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
        => root.TryGetProperty(name, out var value) ? value.GetString() : null;

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

    public async Task<IReadOnlyCollection<BinanceOpenOrder>> GetOpenOrdersAsync(string symbol, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = symbol
        };

        var json = await SendSignedAsync(
            HttpMethod.Get,
            "/fapi/v1/openOrders",
            parameters,
            cancellationToken);

        using var document = JsonDocument.Parse(json);

        return document.RootElement.EnumerateArray()
        .Select(x =>
        {
            var createdTimeMs = TryGetLong(x, "time") ?? 0;
            var updateTimeMs = TryGetLong(x, "updateTime") ?? createdTimeMs;

            return new BinanceOpenOrder
            {
                Symbol = GetString(x, "symbol"),
                OrderId = GetFlexibleString(x, "orderId"),
                ClientOrderId = GetString(x, "clientOrderId"),
                Type = GetString(x, "type"),
                Side = GetString(x, "side"),
                PositionSide = GetString(x, "positionSide"),
                Price = TryGetDecimal(x, "price") ?? 0,
                Quantity = TryGetDecimal(x, "origQty") ?? 0,

                CreatedAtUtc = FromUnixMs(createdTimeMs),
                UpdateTimeUtc = FromUnixMs(updateTimeMs)
            };
        })
        .ToList();
    }

    public async Task<IReadOnlyCollection<BinanceOpenAlgoOrder>> GetOpenAlgoOrdersAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = symbol
        };

        var json = await SendSignedAsync(
            HttpMethod.Get,
            "/fapi/v1/openAlgoOrders",
            parameters,
            cancellationToken);

        using var document = JsonDocument.Parse(json);

        return document.RootElement.EnumerateArray()
            .Select(x => new BinanceOpenAlgoOrder
            {
                Symbol = GetString(x, "symbol"),
                AlgoOrderId = GetFlexibleString(x, "algoId"),
                ClientAlgoId = GetString(x, "clientAlgoId"),
                PositionSide = GetString(x, "positionSide"),
                Status = GetString(x, "algoStatus"),
                TriggerPrice = TryGetDecimal(x, "triggerPrice") ?? 0,
                Quantity = TryGetDecimal(x, "quantity") ?? 0,
                OrderType = GetString(x, "orderType")
            })
            .ToList();
    }

    public async Task SetHedgeModeAsync(CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["dualSidePosition"] = "true"
        };

        await SendSignedAsync(
            HttpMethod.Post,
            "/fapi/v1/positionSide/dual",
            parameters,
            cancellationToken);
    }

    public async Task SetLeverageAsync(string symbol, int leverage, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = symbol,
            ["leverage"] = leverage.ToString(CultureInfo.InvariantCulture)
        };

        await SendSignedAsync(
            HttpMethod.Post,
            "/fapi/v1/leverage",
            parameters,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<BinancePositionRisk>> GetPositionRiskAsync(
    string symbol,
    CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = symbol
        };

        var json = await SendSignedAsync(
            HttpMethod.Get,
            "/fapi/v2/positionRisk",
            parameters,
            cancellationToken);

        using var document = JsonDocument.Parse(json);

        return document.RootElement
            .EnumerateArray()
            .Select(x => new BinancePositionRisk
            {
                Symbol = GetString(x, "symbol"),
                PositionSide = GetString(x, "positionSide"),
                PositionAmount = TryGetDecimal(x, "positionAmt") ?? 0,
                EntryPrice = TryGetDecimal(x, "entryPrice") ?? 0,
                MarkPrice = TryGetDecimal(x, "markPrice") ?? 0
            })
            .ToList();
    }

    public async Task<BinanceOrderResult> GetOrderAsync(
    string symbol,
    string orderId,
    CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = symbol,
            ["orderId"] = orderId
        };

        var json = await SendSignedAsync(
            HttpMethod.Get,
            "/fapi/v1/order",
            parameters,
            cancellationToken);

        return ParseOrderResult(json);
    }

    public async Task<BinanceSymbolFilters> GetSymbolFiltersAsync(string symbol, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = symbol
        };

        var json = await SendUnsignedAsync(
            HttpMethod.Get,
            "/fapi/v1/exchangeInfo",
            parameters,
            cancellationToken);

        using var document = JsonDocument.Parse(json);

        var symbolElement = document.RootElement.GetProperty("symbols")
            .EnumerateArray()
            .First(x => GetString(x, "symbol").Equals(symbol, StringComparison.OrdinalIgnoreCase));

        var priceFilter = symbolElement.GetProperty("filters")
            .EnumerateArray()
            .First(x => GetString(x, "filterType").Equals("PRICE_FILTER", StringComparison.OrdinalIgnoreCase));

        var lotSizeFilter = symbolElement.GetProperty("filters")
            .EnumerateArray()
            .First(x => GetString(x, "filterType").Equals("LOT_SIZE", StringComparison.OrdinalIgnoreCase));

        return new BinanceSymbolFilters
        {
            TickSize = TryGetDecimal(priceFilter, "tickSize") ?? 0,
            StepSize = TryGetDecimal(lotSizeFilter, "stepSize") ?? 0
        };
    }

    private async Task<string> SendUnsignedAsync(
        HttpMethod method,
        string endpoint,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        var query = BuildQueryString(parameters);

        using var request = new HttpRequestMessage(method, $"{endpoint}?{query}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Binance request failed. Status={(int)response.StatusCode}, Body={content}");

        return content;
    }

    private static long? TryGetLong(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
            return number;

        if (value.ValueKind == JsonValueKind.String &&
            long.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return null;
    }

    private static DateTime FromUnixMs(long milliseconds)
    {
        if (milliseconds <= 0)
            return DateTime.UtcNow;

        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
    }
}