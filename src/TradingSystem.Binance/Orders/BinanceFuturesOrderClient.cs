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
    private readonly SemaphoreSlim _timeSyncGate = new(1, 1);

    private long _serverTimeOffsetMilliseconds;

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
        using var document = JsonDocument.Parse(
            await SendSignedAsync(
                HttpMethod.Get, 
                "fapi/v1/openOrders", 
                new() { ["symbol"] = NormalizeSymbol(symbol) }, 
                ct));
        
        return document.RootElement.EnumerateArray()
            .Select(p => new BinanceOpenOrder
            {
                Symbol = StringValue(p, "symbol"),
                OrderId = FlexibleString(p, "orderId"),
                ClientOrderId = StringValue(p, "clientOrderId"),
                Type = StringValue(p, "type"),
                Side = StringValue(p, "side"),
                PositionSide = StringValue(p, "positionSide"),
                Price = NumberValue(p, "price"),
                Quantity = NumberValue(p, "origQty"),
                CreatedAtUtc = UnixTime(LongValue(p, "time")),
                UpdateTimeUtc = UnixTime(LongValue(p, "updateTime") ?? LongValue(p, "time"))
            }).ToArray();
    }

    public async Task<IReadOnlyCollection<BinanceOpenAlgoOrder>> GetOpenAlgoOrdersAsync(string symbol, CancellationToken ct)
    {
        using var document = JsonDocument.Parse(
            await SendSignedAsync(
                HttpMethod.Get, 
                "fapi/v1/openAlgoOrders", 
                new() { ["symbol"] = NormalizeSymbol(symbol) }, 
                ct));
        
        return document.RootElement.EnumerateArray()
            .Select(o => new BinanceOpenAlgoOrder
            {
                Symbol = StringValue(o, "symbol"),
                AlgoOrderId = FlexibleString(o, "algoId"),
                ClientAlgoId = StringValue(o, "clientAlgoId"),
                PositionSide = StringValue(o, "positionSide"),
                Status = StringValue(o, "algoStatus"),
                TriggerPrice = NumberValue(o, "triggerPrice"),
                Quantity = NumberValue(o, "quantity"),
                OrderType = StringValue(o, "orderType")
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
    {
        var currentModeJson = await SendSignedAsync(
            HttpMethod.Get,
            "fapi/v1/positionSide/dual",
            new(),
            ct);

        using (var document = JsonDocument.Parse(currentModeJson))
        {
            if (document.RootElement.TryGetProperty("dualSidePosition", out var dualSidePosition) 
                && dualSidePosition.ValueKind is JsonValueKind.True)
                return;
        }

        try
        {
            _ = await SendSignedAsync(HttpMethod.Post,
                "fapi/v1/positionSide/dual",
                new() { ["dualSidePosition"] = "true" },
                ct);
        }
        catch (BinanceApiException ex) when (IsNoNeedToChangePositionSide(ex))
        {}
    }

    public async Task SetLeverageAsync(string symbol, int leverage, CancellationToken ct)
        => _ = await SendSignedAsync(HttpMethod.Post, 
                "fapi/v1/leverage", 
                new() { ["symbol"] = NormalizeSymbol(symbol), ["leverage"] = leverage.ToString(CultureInfo.InvariantCulture) },
                ct);

    public async Task<IReadOnlyCollection<BinancePositionRisk>> GetPositionRiskAsync(string symbol, CancellationToken ct)
    {
        using var document = JsonDocument.Parse(
            await SendSignedAsync(HttpMethod.Get, 
                "fapi/v2/positionRisk", 
                new() { ["symbol"] = NormalizeSymbol(symbol) },
                ct));
        
        return document.RootElement.EnumerateArray()
            .Select(e => new BinancePositionRisk
            {
                Symbol = StringValue(e, "symbol"),
                PositionSide = StringValue(e, "positionSide"),
                PositionAmount = NumberValue(e, "positionAmt"),
                EntryPrice = NumberValue(e, "entryPrice"),
                MarkPrice = NumberValue(e, "markPrice")
            }).ToArray();
    }

    private async Task<string> SendUnsignedAsync(string endpoint, Dictionary<string, string> parameters, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync($"{endpoint}?{BuildQueryString(parameters)}", ct);
       
        return await ReadResponseAsync(response, endpoint, ct);
    }

    private async Task<string> SendSignedAsync(HttpMethod method, string endpoint, Dictionary<string, string> parameters, CancellationToken ct)
    {
        try
        {
            return await SendSignedOnceAsync(method, endpoint, parameters, ct);
        }
        catch (BinanceApiException ex) when (IsTimestampOutsideReceiveWindow(ex))
        {
            await SynchronizeServerTimeAsync(ct);
            
            return await SendSignedOnceAsync(method, endpoint, parameters, ct);
        }
    }

    private async Task<string> SendSignedOnceAsync(HttpMethod method, string endpoint, Dictionary<string, string> parameters, CancellationToken ct)
    {
        parameters["recvWindow"] = _options.ReceiveWindow.ToString(CultureInfo.InvariantCulture);
        parameters["timestamp"] = CurrentBinanceTimestampMilliseconds().ToString(CultureInfo.InvariantCulture);

        var query = BuildQueryString(parameters);
        var signedQuery = $"{query}&signature={Sign(query)}";

        using var request = new HttpRequestMessage(method, $"{endpoint}?{signedQuery}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        using var response = await _httpClient.SendAsync(request, ct);
       
        return await ReadResponseAsync(response, endpoint, ct);
    }

    private async Task SynchronizeServerTimeAsync(CancellationToken ct)
    {
        await _timeSyncGate.WaitAsync(ct);

        try
        {
            var requestStartedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            using var document = JsonDocument.Parse(await SendUnsignedAsync("fapi/v1/time", new(), ct));

            var requestCompletedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (!document.RootElement.TryGetProperty("serverTime", out var serverTimeElement) 
                || !serverTimeElement.TryGetInt64(out var serverTime))
                throw new InvalidOperationException("Binance server time response does not contain a valid serverTime.");

            var localMidpoint = requestStartedAt + ((requestCompletedAt - requestStartedAt) / 2);
            Interlocked.Exchange(ref _serverTimeOffsetMilliseconds, serverTime - localMidpoint);
        }
        finally
        {
            _timeSyncGate.Release();
        }
    }

    private long CurrentBinanceTimestampMilliseconds()
        => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + Interlocked.Read(ref _serverTimeOffsetMilliseconds);

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
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

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
        var r = document.RootElement;

        return new BinanceOrderResult
        {
            Symbol = StringValue(r, "symbol"),
            ClientOrderId = StringValue(r, "clientOrderId"),
            OrderId = FlexibleString(r, "orderId"),
            Status = StringValue(r, "status"),
            AveragePrice = NumberValue(r, "avgPrice"),
            ExecutedQuantity = NumberValue(r, "executedQty"),
            CumulativeQuoteQuantity = NumberValue(r, "cumQuote")
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

    private static bool IsTimestampOutsideReceiveWindow(BinanceApiException exception)
        => exception.ResponseBody.Contains("\"code\":-1021", StringComparison.Ordinal);

    private static bool IsNoNeedToChangePositionSide(BinanceApiException exception)
        => exception.ResponseBody.Contains("\"code\":-4059", StringComparison.Ordinal);
}
