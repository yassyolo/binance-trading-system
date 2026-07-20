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
    private readonly BinanceFuturesOptions _options;

    public BinanceFuturesOrderClient(HttpClient httpClient,  IOptions<BinanceFuturesOptions> options)
    {
        _httpClient  =  httpClient;
        _options  =  options.Value;
        _httpClient.DefaultRequestHeaders.Remove("X-MBX-APIKEY");
        _httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY",  _options.ApiKey);
    }

    public Task<BinanceOrderResult> PlaceMarketOrderAsync(string symbol,  string side,  string positionSide,  decimal quantity,  string clientOrderId,  CancellationToken ct)
         =>  PlaceOrderAsync(symbol,  side,  positionSide,  "MARKET",  quantity,  clientOrderId,  null,  ct);

    public Task<BinanceOrderResult> PlaceLimitOrderAsync(string symbol,  string side,  string positionSide,  decimal quantity,  decimal price,  string clientOrderId,  CancellationToken ct)
         =>  PlaceOrderAsync(symbol,  side,  positionSide,  "LIMIT",  quantity,  clientOrderId,  price,  ct);

    private async Task<BinanceOrderResult> PlaceOrderAsync(string symbol,  string side,  string positionSide,  string type,  decimal quantity,  string clientOrderId,  decimal? price,  CancellationToken ct)
    {
        var p  =  new Dictionary<string,  string>
        {
            ["symbol"]  =  symbol.ToUpperInvariant(),  ["side"]  =  side,  ["positionSide"]  =  positionSide, 
            ["type"]  =  type,  ["quantity"]  =  D(quantity),  ["newClientOrderId"]  =  clientOrderId,  ["newOrderRespType"]  =  "RESULT"
        };
        if (price.HasValue) { p["timeInForce"]  =  "GTC"; p["price"]  =  D(price.Value); }
        return ParseOrder(await SendSignedAsync(HttpMethod.Post,  "/fapi/v1/order",  p,  ct));
    }

    public Task<BinanceAlgoOrderResult> PlaceTakeProfitMarketAlgoOrderAsync(string symbol,  string side,  string positionSide,  decimal quantity,  decimal stopPrice,  string clientOrderId,  CancellationToken ct)
         =>  PlaceAlgoAsync(symbol,  side,  positionSide,  "TAKE_PROFIT_MARKET",  quantity,  stopPrice,  clientOrderId,  ct);

    public Task<BinanceAlgoOrderResult> PlaceStopMarketAlgoOrderAsync(string symbol,  string side,  string positionSide,  decimal quantity,  decimal stopPrice,  string clientOrderId,  CancellationToken ct)
         =>  PlaceAlgoAsync(symbol,  side,  positionSide,  "STOP_MARKET",  quantity,  stopPrice,  clientOrderId,  ct);

    private async Task<BinanceAlgoOrderResult> PlaceAlgoAsync(string symbol,  string side,  string positionSide,  string type,  decimal quantity,  decimal trigger,  string clientId,  CancellationToken ct)
    {
        var p  =  new Dictionary<string,  string>
        {
            ["symbol"]  =  symbol.ToUpperInvariant(),  ["side"]  =  side,  ["positionSide"]  =  positionSide, 
            ["algoType"]  =  "CONDITIONAL",  ["type"]  =  type,  ["quantity"]  =  D(quantity), 
            ["triggerPrice"]  =  D(trigger),  ["workingType"]  =  "MARK_PRICE",  ["clientAlgoId"]  =  clientId
        };
        return ParseAlgo(await SendSignedAsync(HttpMethod.Post,  "/fapi/v1/algoOrder",  p,  ct));
    }

    public async Task<BinanceOrderResult> GetOrderAsync(string symbol,  string orderId,  CancellationToken ct)
         =>  ParseOrder(await SendSignedAsync(HttpMethod.Get,  "/fapi/v1/order",  new() { ["symbol"]  =  symbol,  ["orderId"]  =  orderId },  ct));

    public async Task CancelOrderAsync(string symbol,  string orderId,  CancellationToken ct)
         =>  _  =  await SendSignedAsync(HttpMethod.Delete,  "/fapi/v1/order",  new() { ["symbol"]  =  symbol,  ["orderId"]  =  orderId },  ct);

    public async Task CancelAlgoOrderAsync(string symbol,  string algoOrderId,  CancellationToken ct)
         =>  _  =  await SendSignedAsync(HttpMethod.Delete,  "/fapi/v1/algoOrder",  new() { ["symbol"]  =  symbol,  ["algoId"]  =  algoOrderId },  ct);

    public async Task<IReadOnlyCollection<BinanceOpenOrder>> GetOpenOrdersAsync(string symbol,  CancellationToken ct)
    {
        using var d  =  JsonDocument.Parse(await SendSignedAsync(HttpMethod.Get,  "/fapi/v1/openOrders",  new() { ["symbol"]  =  symbol },  ct));
        return d.RootElement.EnumerateArray().Select(x  =>  new BinanceOpenOrder
        {
            Symbol  =  S(x, "symbol"),  OrderId  =  FS(x, "orderId"),  ClientOrderId  =  S(x, "clientOrderId"),  Type  =  S(x, "type"), 
            Side  =  S(x, "side"),  PositionSide  =  S(x, "positionSide"),  Price  =  N(x, "price"),  Quantity  =  N(x, "origQty"), 
            CreatedAtUtc  =  T(L(x, "time")),  UpdateTimeUtc  =  T(L(x, "updateTime") ?? L(x, "time"))
        }).ToArray();
    }

    public async Task<IReadOnlyCollection<BinanceOpenAlgoOrder>> GetOpenAlgoOrdersAsync(string symbol,  CancellationToken ct)
    {
        using var d  =  JsonDocument.Parse(await SendSignedAsync(HttpMethod.Get,  "/fapi/v1/openAlgoOrders",  new() { ["symbol"]  =  symbol },  ct));
        return d.RootElement.EnumerateArray().Select(x  =>  new BinanceOpenAlgoOrder
        {
            Symbol  =  S(x, "symbol"),  AlgoOrderId  =  FS(x, "algoId"),  ClientAlgoId  =  S(x, "clientAlgoId"),  PositionSide  =  S(x, "positionSide"), 
            Status  =  S(x, "algoStatus"),  TriggerPrice  =  N(x, "triggerPrice"),  Quantity  =  N(x, "quantity"),  OrderType  =  S(x, "orderType")
        }).ToArray();
    }

    public async Task<BinanceSymbolFilters> GetSymbolFiltersAsync(string symbol,  CancellationToken ct)
    {
        using var d  =  JsonDocument.Parse(await SendUnsignedAsync("/fapi/v1/exchangeInfo",  new() { ["symbol"]  =  symbol },  ct));
        var s  =  d.RootElement.GetProperty("symbols").EnumerateArray().First(x  =>  S(x, "symbol").Equals(symbol,  StringComparison.OrdinalIgnoreCase));
        var filters  =  s.GetProperty("filters").EnumerateArray().ToArray();
        var price  =  filters.First(x  =>  S(x, "filterType") == "PRICE_FILTER");
        var lot  =  filters.First(x  =>  S(x, "filterType") == "LOT_SIZE");
        return new() { TickSize  =  N(price, "tickSize"),  StepSize  =  N(lot, "stepSize"),  MinQuantity  =  N(lot, "minQty") };
    }

    public async Task SetHedgeModeAsync(CancellationToken ct)
         =>  _  =  await SendSignedAsync(HttpMethod.Post,  "/fapi/v1/positionSide/dual",  new() { ["dualSidePosition"]  =  "true" },  ct);

    public async Task SetLeverageAsync(string symbol,  int leverage,  CancellationToken ct)
         =>  _  =  await SendSignedAsync(HttpMethod.Post,  "/fapi/v1/leverage",  new() { ["symbol"]  =  symbol,  ["leverage"]  =  leverage.ToString(CultureInfo.InvariantCulture) },  ct);

    public async Task<IReadOnlyCollection<BinancePositionRisk>> GetPositionRiskAsync(string symbol,  CancellationToken ct)
    {
        using var d  =  JsonDocument.Parse(await SendSignedAsync(HttpMethod.Get,  "/fapi/v2/positionRisk",  new() { ["symbol"]  =  symbol },  ct));
        return d.RootElement.EnumerateArray().Select(x  =>  new BinancePositionRisk
        { Symbol = S(x, "symbol"),  PositionSide = S(x, "positionSide"),  PositionAmount = N(x, "positionAmt"),  EntryPrice = N(x, "entryPrice"),  MarkPrice = N(x, "markPrice") }).ToArray();
    }

    private async Task<string> SendUnsignedAsync(string endpoint,  Dictionary<string, string> p,  CancellationToken ct)
    {
        using var response  =  await _httpClient.GetAsync($"{endpoint}?{Q(p)}",  ct);
        var body  =  await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new BinanceApiException((int)response.StatusCode,  body);
        return body;
    }

    private async Task<string> SendSignedAsync(HttpMethod method,  string endpoint,  Dictionary<string, string> p,  CancellationToken ct)
    {
        p["recvWindow"]  =  _options.ReceiveWindow.ToString(CultureInfo.InvariantCulture);
        p["timestamp"]  =  DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        var query  =  Q(p); var signed  =  $"{query}&signature = {Sign(query)}";
        using var request  =  new HttpRequestMessage(method,  $"{endpoint}?{signed}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response  =  await _httpClient.SendAsync(request,  ct);
        var body  =  await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new BinanceApiException((int)response.StatusCode,  body);
        return body;
    }

    private string Sign(string q) { using var h  =  new HMACSHA256(Encoding.UTF8.GetBytes(_options.SecretKey)); return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(q))).ToLowerInvariant(); }
    private static string Q(Dictionary<string, string> p)  =>  string.Join("&",  p.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{Uri.EscapeDataString(x.Key)} = {Uri.EscapeDataString(x.Value)}"));
    private static string D(decimal v)  =>  v.ToString("0.########",  CultureInfo.InvariantCulture);
    private static string S(JsonElement e, string n)  =>  e.TryGetProperty(n, out var v) ? v.GetString() ?? string.Empty : string.Empty;
    private static string FS(JsonElement e, string n)  =>  !e.TryGetProperty(n, out var v) ? string.Empty : v.ValueKind==JsonValueKind.String ? v.GetString() ?? string.Empty : v.GetRawText();
    private static decimal N(JsonElement e, string n) { if(!e.TryGetProperty(n, out var v)) return 0; if(v.ValueKind==JsonValueKind.Number  &&  v.TryGetDecimal(out var d)) return d; return decimal.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out d)?d:0; }
    private static long? L(JsonElement e, string n) { if(!e.TryGetProperty(n, out var v)) return null; if(v.ValueKind==JsonValueKind.Number  &&  v.TryGetInt64(out var l)) return l; return long.TryParse(v.GetString(), out l)?l:null; }
    private static DateTime T(long? ms)  =>  ms is > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(ms.Value).UtcDateTime : DateTime.UnixEpoch;
    private static BinanceOrderResult ParseOrder(string json) { using var d = JsonDocument.Parse(json); var r = d.RootElement; return new(){Symbol = S(r, "symbol"), ClientOrderId = S(r, "clientOrderId"), OrderId = FS(r, "orderId"), Status = S(r, "status"), AveragePrice = N(r, "avgPrice"), ExecutedQuantity = N(r, "executedQty"), CumulativeQuoteQuantity = N(r, "cumQuote")}; }
    private static BinanceAlgoOrderResult ParseAlgo(string json) { using var d = JsonDocument.Parse(json); var r = d.RootElement; return new(){Symbol = S(r, "symbol"), ClientOrderId = S(r, "clientAlgoId"), AlgoOrderId = FS(r, "algoId"), Status = S(r, "status")}; }
}
