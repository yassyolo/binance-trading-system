
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

const string baseUrl = "https://fapi.binance.com";
var symbol = Get("--symbol", "BTCUSDC").ToUpperInvariant();
var interval = Get("--interval", "1m");
var startUtc = ParseUtc(Get("--from", "2026-01-01"));
var endUtc = ParseUtc(Get("--to", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
var output = Get("--output", $"data/{symbol}_{interval}_{startUtc:yyyyMMdd}_{endUtc:yyyyMMdd}.csv");
var append = Has("--append");

if (endUtc <= startUtc)
    throw new ArgumentException("--to must be after --from.");

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);

using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("TradingSystem-HistoricalData/1.0");

var existingLastOpenTime = append && File.Exists(output)
    ? ReadLastOpenTime(output)
    : null;

if (existingLastOpenTime.HasValue && existingLastOpenTime.Value >= startUtc)
    startUtc = existingLastOpenTime.Value.AddMilliseconds(1);

await using var stream = new FileStream(
    output,
    append && File.Exists(output) ? FileMode.Append : FileMode.Create,
    FileAccess.Write,
    FileShare.Read);

await using var writer = new StreamWriter(stream);

if (stream.Length == 0)
    await writer.WriteLineAsync("open_time_utc,close_time_utc,open,high,low,close,volume");

var cursor = new DateTimeOffset(startUtc).ToUnixTimeMilliseconds();
var endMs = new DateTimeOffset(endUtc).ToUnixTimeMilliseconds();
var total = 0;

while (cursor < endMs)
{
    var endpoint =
        $"/fapi/v1/klines?symbol={Uri.EscapeDataString(symbol)}" +
        $"&interval={Uri.EscapeDataString(interval)}&startTime={cursor}&endTime={endMs}&limit=1500";

    using var response = await http.GetAsync(endpoint);
    var body = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
        throw new InvalidOperationException(
            $"Binance request failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");

    using var document = JsonDocument.Parse(body);
    var rows = document.RootElement;

    if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() == 0)
        break;

    long lastOpenTime = cursor;

    foreach (var row in rows.EnumerateArray())
    {
        var values = row.EnumerateArray().ToArray();
        if (values.Length < 7)
            continue;

        var openTime = values[0].GetInt64();
        var closeTime = values[6].GetInt64();

        await writer.WriteLineAsync(string.Join(",",
            DateTimeOffset.FromUnixTimeMilliseconds(openTime).UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset.FromUnixTimeMilliseconds(closeTime).UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            values[1].GetString(),
            values[2].GetString(),
            values[3].GetString(),
            values[4].GetString(),
            values[5].GetString()));

        lastOpenTime = openTime;
        total++;
    }

    await writer.FlushAsync();

    var next = lastOpenTime + 1;
    if (next <= cursor)
        break;

    cursor = next;
    Console.WriteLine($"Downloaded {total:N0} candles through {DateTimeOffset.FromUnixTimeMilliseconds(lastOpenTime):u}");

    await Task.Delay(150);
}

Console.WriteLine($"Completed. Candles={total:N0}; File={Path.GetFullPath(output)}");

string Get(string name, string fallback)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
}

bool Has(string name) => args.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase));

static DateTime ParseUtc(string value)
{
    if (!DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var result))
        throw new ArgumentException($"Invalid UTC date: {value}");

    return DateTime.SpecifyKind(result, DateTimeKind.Utc);
}

static DateTime? ReadLastOpenTime(string path)
{
    var last = File.ReadLines(path)
        .LastOrDefault(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("open_time_utc"));

    if (last is null)
        return null;

    var first = last.Split(',', 2)[0];

    return DateTime.TryParse(
        first,
        CultureInfo.InvariantCulture,
        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
        out var parsed)
        ? parsed
        : null;
}
