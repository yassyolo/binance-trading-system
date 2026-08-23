using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Exceptions;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Orders.Models;
using TradingSystem.Binance.Positions;

namespace TradingSystem.Binance.Orders;

public sealed class BinanceFuturesOrderClient : IBinanceFuturesOrderClient
{
    private readonly HttpClient _httpClient;
    private readonly BinanceFuturesOptions _options;

    public BinanceFuturesOrderClient(HttpClient httpClient, IOptions<BinanceFuturesOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.DefaultRequestHeaders.Remove("X-MBX-APIKEY");
        _httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _options.ApiKey);
    }

    public Task<BinanceOrderResult> PlaceMarketOrderAsync(string symbol, string side, string positionSide, decimal quantity, string clientOrderId, CancellationToken ct)
        => PlaceOrderAsync(symbol, side, positionSide, "MARKET", quantity, clientOrderId, null, ct);

    public Task<BinanceOrderResult> PlaceLimitOrderAsync(string symbol, string side, string positionSide, decimal quantity, decimal price, string clientOrderId, CancellationToken ct)
        => PlaceOrderAsync(symbol, side, positionSide, "LIMIT", quantity, clientOrderId, price, ct);

    private async Task<BinanceOrderResult> PlaceOrderAsync(string symbol, string side, string positionSide, string type, decimal quantity, string clientOrderId, decimal? price, CancellationToken ct)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = NormalizeSymbol(symbol),
            ["side"] = side,
            ["positionSide"] = positionSide,
            ["type"] = type,
            ["quantity"] = DecimalString(quantity),
            ["newClientOrderId"] = clientOrderId,
            ["newOrderRespType"] = "RESULT"
        };

        if (price.HasValue)
        {
            parameters["timeInForce"] = "GTC";
            parameters["price"] = DecimalString(price.Value);
        }

        return ParseOrder(await SendSignedAsync(HttpMethod.Post, "fapi/v1/order", parameters, ct));
    }

    public Task<BinanceAlgoOrderResult> PlaceTakeProfitMarketAlgoOrderAsync(string symbol, string side, string positionSide, decimal quantity, decimal stopPrice, string clientOrderId, CancellationToken ct)
        => PlaceAlgoAsync(symbol, side, positionSide, "TAKE_PROFIT_MARKET", quantity, stopPrice, clientOrderId, ct);

    public Task<BinanceAlgoOrderResult> PlaceStopMarketAlgoOrderAsync(string symbol, string side, string positionSide, decimal quantity, decimal stopPrice, string clientOrderId, CancellationToken ct)
        => PlaceAlgoAsync(symbol, side, positionSide, "STOP_MARKET", quantity, stopPrice, clientOrderId, ct);

    private async Task<BinanceAlgoOrderResult> PlaceAlgoAsync(string symbol, string side, string positionSide, string type, decimal quantity, decimal trigger, string clientId, CancellationToken ct)
    {
        var parameters = new Dictionary<string, string>
        {
            ["symbol"] = NormalizeSymbol(symbol),
            ["side"] = side,
            ["positionSide"] = positionSide,
            ["algoType"] = "CONDITIONAL",
            ["type"] = type,
            ["quantity"] = DecimalString(quantity),
            ["triggerPrice"] = DecimalString(trigger),
            ["workingType"] = "MARK_PRICE",
            ["clientAlgoId"] = clientId
        };

        return ParseAlgo(await SendSignedAsync(HttpMethod.Post, "fapi/v1/algoOrder", parameters, ct));
    }

    public async Task<BinanceOrderResult> GetOrderAsync(string symbol, string orderId, CancellationToken ct)
        => ParseOrder(await SendSignedAsync(
            HttpMethod.Get,
            "fapi/v1/order",
            new()
            {
                ["symbol"] = NormalizeSymbol(symbol),
                ["orderId"] = orderId
            },
            ct));

    public async Task<BinanceOrderResult> GetOrderByClientOrderIdAsync(string symbol, string clientOrderId, CancellationToken ct)
        => ParseOrder(await SendSignedAsync(
            HttpMethod.Get,
            "fapi/v1/order",
            new()
            {
                ["symbol"] = NormalizeSymbol(symbol),
                ["origClientOrderId"] = clientOrderId
            },
            ct));

    public async Task CancelOrderAsync(string symbol, string orderId, CancellationToken ct)
        => _ = await SendSignedAsync(HttpMethod.Delete, "fapi/v1/order", new() { ["symbol"] = NormalizeSymbol(symbol), ["orderId"] = orderId }, ct);

    public async Task CancelAlgoOrderAsync(string symbol, string algoOrderId, CancellationToken ct)
        => _ = await SendSignedAsync(HttpMethod.Delete, "fapi/v1/algoOrder", new() { ["symbol"] = NormalizeSymbol(symbol), ["algoId"] = algoOrderId }, ct);

    public async Task<IReadOnlyCollection<BinanceOpenOrder>> GetOpenOrdersAsync(string symbol, CancellationToken ct)
    {
        using var document = JsonDocument.Parse(await SendSignedAsync(HttpMethod.Get, "fapi/v1/openOrders", new() { ["symbol"] = NormalizeSymbol(symbol) }, ct));
        return document.RootElement.EnumerateArray().Select(element => new BinanceOpenOrder
        {
            Symbol = StringValue(element, "symbol"),
            OrderId = FlexibleString(element, "orderId"),
            ClientOrderId = StringValue(element, "clientOrderId"),
            Type = StringValue(element, "type"),
            Side = StringValue(element, "side"),
            PositionSide = StringValue(element, "positionSide"),
            Price = NumberValue(element, "price"),
            Quantity = NumberValue(element, "origQty"),
            CreatedAtUtc = UnixTime(LongValue(element, "time")),
            UpdateTimeUtc = UnixTime(LongValue(element, "updateTime") ?? LongValue(element, "time"))
        }).ToArray();
    }

    public async Task<IReadOnlyCollection<BinanceOpenAlgoOrder>> GetOpenAlgoOrdersAsync(string symbol, CancellationToken ct)
    {
        using var document = JsonDocument.Parse(await SendSignedAsync(HttpMethod.Get, "fapi/v1/openAlgoOrders", new() { ["symbol"] = NormalizeSymbol(symbol) }, ct));
        return document.RootElement.EnumerateArray().Select(element => new BinanceOpenAlgoOrder
        {
            Symbol = StringValue(element, "symbol"),
            AlgoOrderId = FlexibleString(element, "algoId"),
            ClientAlgoId = StringValue(element, "clientAlgoId"),
            PositionSide = StringValue(element, "positionSide"),
            Status = StringValue(element, "algoStatus"),
            TriggerPrice = NumberValue(element, "triggerPrice"),
            Quantity = NumberValue(element, "quantity"),
            OrderType = StringValue(element, "orderType")
        }).ToArray();
    }

    public async Task<BinanceSymbolFilters> GetSymbolFiltersAsync(string symbol, CancellationToken ct)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        using var document = JsonDocument.Parse(await SendUnsignedAsync("fapi/v1/exchangeInfo", new() { ["symbol"] = normalizedSymbol }, ct));
        var symbolElement = document.RootElement.GetProperty("symbols").EnumerateArray()
            .FirstOrDefault(element => StringValue(element, "symbol").Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase));

        if (symbolElement.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException($"Binance exchange info does not contain symbol '{normalizedSymbol}'.");

        var filters = symbolElement.GetProperty("filters").EnumerateArray().ToArray();
        var priceFilter = filters.FirstOrDefault(element => StringValue(element, "filterType") == "PRICE_FILTER");
        var lotSizeFilter = filters.FirstOrDefault(element => StringValue(element, "filterType") == "LOT_SIZE");

        if (priceFilter.ValueKind == JsonValueKind.Undefined || lotSizeFilter.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException($"Required exchange filters are missing for symbol '{normalizedSymbol}'.");

        return new BinanceSymbolFilters
        {
            TickSize = NumberValue(priceFilter, "tickSize"),
            StepSize = NumberValue(lotSizeFilter, "stepSize"),
            MinQuantity = NumberValue(lotSizeFilter, "minQty")
        };
    }

    public async Task SetHedgeModeAsync(CancellationToken ct)
        => _ = await SendSignedAsync(HttpMethod.Post, "fapi/v1/positionSide/dual", new() { ["dualSidePosition"] = "true" }, ct);

    public async Task SetLeverageAsync(string symbol, int leverage, CancellationToken ct)
        => _ = await SendSignedAsync(HttpMethod.Post, "fapi/v1/leverage", new() { ["symbol"] = NormalizeSymbol(symbol), ["leverage"] = leverage.ToString(CultureInfo.InvariantCulture) }, ct);

    public async Task<IReadOnlyCollection<BinancePositionRisk>> GetPositionRiskAsync(string symbol, CancellationToken ct)
    {
        using var document = JsonDocument.Parse(await SendSignedAsync(HttpMethod.Get, "fapi/v2/positionRisk", new() { ["symbol"] = NormalizeSymbol(symbol) }, ct));
        return document.RootElement.EnumerateArray().Select(element => new BinancePositionRisk
        {
            Symbol = StringValue(element, "symbol"),
            PositionSide = StringValue(element, "positionSide"),
            PositionAmount = NumberValue(element, "positionAmt"),
            EntryPrice = NumberValue(element, "entryPrice"),
            MarkPrice = NumberValue(element, "markPrice")
        }).ToArray();
    }

    private async Task<string> SendUnsignedAsync(string endpoint, Dictionary<string, string> parameters, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync($"{endpoint}?{BuildQueryString(parameters)}", ct);
        return await ReadResponseAsync(response, endpoint, ct);
    }

    private async Task<string> SendSignedAsync(HttpMethod method, string endpoint, Dictionary<string, string> parameters, CancellationToken ct)
    {
        parameters["recvWindow"] = _options.ReceiveWindow.ToString(CultureInfo.InvariantCulture);
        parameters["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        var query = BuildQueryString(parameters);
        var signedQuery = $"{query}&signature={Sign(query)}";

        using var request = new HttpRequestMessage(method, $"{endpoint}?{signedQuery}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _httpClient.SendAsync(request, ct);
        return await ReadResponseAsync(response, endpoint, ct);
    }

    private static async Task<string> ReadResponseAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new BinanceApiException(response.StatusCode, body, operation);

        return body;
    }

    private string Sign(string query)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SecretKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(query))).ToLowerInvariant();
    }

    private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> parameters)
        => string.Join("&", parameters.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

    private static string NormalizeSymbol(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        return symbol.Trim().ToUpperInvariant();
    }

    private static string DecimalString(decimal value)
        => value.ToString("0.########", CultureInfo.InvariantCulture);

    private static string StringValue(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static string FlexibleString(JsonElement element, string name)
        => !element.TryGetProperty(name, out var value)
            ? string.Empty
            : value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : value.GetRawText();

    private static decimal NumberValue(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            return 0;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            return number;

        return decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number)
            ? number
            : 0;
    }

    private static long? LongValue(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
            return number;

        return long.TryParse(value.GetString(), out number)
            ? number
            : null;
    }

    private static DateTime UnixTime(long? milliseconds)
        => milliseconds is > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds.Value).UtcDateTime
            : DateTime.UnixEpoch;

    private static BinanceOrderResult ParseOrder(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new BinanceOrderResult
        {
            Symbol = StringValue(root, "symbol"),
            ClientOrderId = StringValue(root, "clientOrderId"),
            OrderId = FlexibleString(root, "orderId"),
            Status = StringValue(root, "status"),
            AveragePrice = NumberValue(root, "avgPrice"),
            ExecutedQuantity = NumberValue(root, "executedQty"),
            CumulativeQuoteQuantity = NumberValue(root, "cumQuote")
        };
    }

    private static BinanceAlgoOrderResult ParseAlgo(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new BinanceAlgoOrderResult
        {
            Symbol = StringValue(root, "symbol"),
            ClientOrderId = StringValue(root, "clientAlgoId"),
            AlgoOrderId = FlexibleString(root, "algoId"),
            Status = StringValue(root, "status")
        };
    }

    public async Task<decimal> GetMarkPriceAsync(string symbol, CancellationToken ct)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);

        using var document = JsonDocument.Parse(
            await SendUnsignedAsync(
                "fapi/v1/premiumIndex",
                new() { ["symbol"] = normalizedSymbol },
                ct));

        return NumberValue(document.RootElement, "markPrice");
    }

    public async Task<IReadOnlyCollection<BinanceTradeFill>> GetTradeFillsForOrderAsync(
    string symbol,
    string orderId,
    CancellationToken ct)
    {
        using var document = JsonDocument.Parse(
            await SendSignedAsync(
                HttpMethod.Get,
                "fapi/v1/userTrades",
                new()
                {
                    ["symbol"] = NormalizeSymbol(symbol),
                    ["orderId"] = orderId
                },
                ct));

        return document.RootElement
            .EnumerateArray()
            .Select(element => new BinanceTradeFill
            {
                Symbol = StringValue(element, "symbol"),
                OrderId = FlexibleString(element, "orderId"),
                Price = NumberValue(element, "price"),
                Quantity = NumberValue(element, "qty"),
                QuoteQuantity = NumberValue(element, "quoteQty")
            })
            .ToArray();
    }
}
