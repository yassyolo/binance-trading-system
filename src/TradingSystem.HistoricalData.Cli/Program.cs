using System.Globalization;
using TradingSystem.Binance.Market;

var options = Parse(args);
var symbol = Get("symbol", "BTCUSDC").Trim().ToUpperInvariant();
var interval = Get("interval", "1m").Trim().ToLowerInvariant();
var fromUtc = ParseUtc(Get("from", DateTime.UtcNow.AddMonths(-1)
    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
var toUtc = ParseUtc(Get("to", DateTime.UtcNow
    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
var environment = Get("environment", "Production");
var output = Get("output", $"data/{symbol}_{interval}_{fromUtc:yyyyMMdd}_{toUtc:yyyyMMdd}.csv");

Validate(symbol, interval, fromUtc, toUtc, output);

var baseAddress = environment.Equals("Demo", StringComparison.OrdinalIgnoreCase)
    ? "https://demo-fapi.binance.com"
    : environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
        ? "https://fapi.binance.com"
        : throw new ArgumentException("environment must be Demo or Production.");

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

using var http = new HttpClient
{
    BaseAddress = new Uri(baseAddress),
    Timeout = TimeSpan.FromSeconds(30)
};

var source = new BinanceHistoricalCandleRangeSource(http);
var candles = await source.LoadAsync(symbol, interval, fromUtc, toUtc, cancellation.Token);
var ordered = candles
    .Where(candle => candle.OpenTimeUtc >= fromUtc && candle.OpenTimeUtc < toUtc)
    .OrderBy(candle => candle.OpenTimeUtc)
    .GroupBy(candle => candle.OpenTimeUtc)
    .Select(group => group.Last())
    .ToArray();

var fullOutputPath = Path.GetFullPath(output);
Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
var temporaryPath = fullOutputPath + ".tmp";

try
{
    await using (var writer = new StreamWriter(temporaryPath, false, new System.Text.UTF8Encoding(false)))
    {
        await writer.WriteLineAsync("open_time_utc,close_time_utc,open,high,low,close,volume");
        foreach (var candle in ordered)
        {
            cancellation.Token.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(string.Join(',',
                candle.OpenTimeUtc.ToString("O", CultureInfo.InvariantCulture),
                candle.CloseTimeUtc.ToString("O", CultureInfo.InvariantCulture),
                Format(candle.Open),
                Format(candle.High),
                Format(candle.Low),
                Format(candle.Close),
                Format(candle.Volume)));
        }
    }

    File.Move(temporaryPath, fullOutputPath, overwrite: true);
    Console.WriteLine(
        $"Completed. Environment = {environment}; Candles = {ordered.Length:N0}; File = {fullOutputPath}");
}
catch
{
    if (File.Exists(temporaryPath))
        File.Delete(temporaryPath);
    throw;
}

string Get(string key, string fallback) => options.TryGetValue(key, out var value) ? value : fallback;
static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);

static DateTime ParseUtc(string value)
{
    if (!DateTime.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
        throw new ArgumentException($"Invalid UTC date: {value}");

    return DateTime.SpecifyKind(result, DateTimeKind.Utc);
}

static void Validate(string symbol, string interval, DateTime fromUtc, DateTime toUtc, string output)
{
    if (string.IsNullOrWhiteSpace(symbol) || !symbol.All(char.IsLetterOrDigit))
        throw new ArgumentException("symbol must contain only letters and digits.");
    if (string.IsNullOrWhiteSpace(interval))
        throw new ArgumentException("interval is required.");
    if (fromUtc >= toUtc)
        throw new ArgumentException("The export range must satisfy from < to.");
    if (toUtc - fromUtc > TimeSpan.FromDays(3660))
        throw new ArgumentException("The export range cannot exceed 10 years.");
    if (string.IsNullOrWhiteSpace(output))
        throw new ArgumentException("output is required.");
}

static Dictionary<string, string> Parse(string[] values)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var index = 0; index < values.Length; index++)
    {
        if (!values[index].StartsWith("--", StringComparison.Ordinal))
            continue;

        var key = values[index][2..];
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Command-line option names cannot be empty.");
        if (!result.TryAdd(key,
                index + 1 < values.Length && !values[index + 1].StartsWith("--", StringComparison.Ordinal)
                    ? values[++index]
                    : "true"))
            throw new ArgumentException($"Duplicate command-line option '--{key}'.");
    }
    return result;
}
