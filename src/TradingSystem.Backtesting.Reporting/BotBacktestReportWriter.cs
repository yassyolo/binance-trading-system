using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using TradingSystem.Backtesting.Bots.Common;

namespace TradingSystem.Backtesting.Reporting;

public sealed class BotBacktestReportWriter
{
    private const int MaximumChartPoints = 5_000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<string> WriteAsync<TOptions>(BotBacktestResult<TOptions> result, string outputRoot, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (string.IsNullOrWhiteSpace(outputRoot))
            throw new ArgumentException("Output root is required.", nameof(outputRoot));

        var directory = Path.Combine(outputRoot, Sanitize(result.RunId));
        Directory.CreateDirectory(directory);

        await WriteAtomicAsync(Path.Combine(directory, "result.json"), JsonSerializer.Serialize(result, JsonOptions), ct);
        await WriteAtomicAsync(Path.Combine(directory, "positions.csv"), PositionsCsv(result), ct);
        await WriteAtomicAsync(Path.Combine(directory, "executions.csv"), ExecutionsCsv(result), ct);
        await WriteAtomicAsync(Path.Combine(directory, "decisions.csv"), DecisionsCsv(result), ct);
        await WriteAtomicAsync(Path.Combine(directory, "report.html"), Html(result), ct);
        return directory;
    }

    private static string PositionsCsv<TOptions>(BotBacktestResult<TOptions> result)
    {
        var builder = new StringBuilder("id,side,entry_time,entry_price,exit_time,exit_price,quantity,gross_pnl,fees,net_pnl,partial_tp,exit_reason\n");
        foreach (var position in result.Positions)
        {
            builder.AppendLine(Csv(
                position.PositionId,
                position.Side,
                position.EntryTimeUtc.ToString("O", CultureInfo.InvariantCulture),
                F(position.EntryPrice),
                position.ExitTimeUtc.ToString("O", CultureInfo.InvariantCulture),
                F(position.ExitPrice),
                F(position.Quantity),
                F(position.GrossPnl),
                F(position.Fees),
                F(position.NetPnl),
                position.PartialTakeProfitReached,
                position.ExitReason));
        }
        return builder.ToString();
    }

    private static string ExecutionsCsv<TOptions>(BotBacktestResult<TOptions> result)
    {
        var builder = new StringBuilder("position_id,time,type,side,price,quantity,gross_pnl,fee,reason\n");
        foreach (var execution in result.Executions)
        {
            builder.AppendLine(Csv(
                execution.PositionId,
                execution.TimeUtc.ToString("O", CultureInfo.InvariantCulture),
                execution.Type,
                execution.Side,
                F(execution.Price),
                F(execution.Quantity),
                F(execution.GrossPnl),
                F(execution.Fee),
                execution.Reason));
        }
        return builder.ToString();
    }

    private static string DecisionsCsv<TOptions>(BotBacktestResult<TOptions> result)
    {
        var builder = new StringBuilder("time,side,decision,reason,reference_price,signal_id\n");
        foreach (var decision in result.Decisions)
        {
            builder.AppendLine(Csv(
                decision.TimeUtc.ToString("O", CultureInfo.InvariantCulture),
                decision.Side,
                decision.Decision,
                decision.Reason,
                F(decision.ReferencePrice),
                decision.SignalId));
        }
        return builder.ToString();
    }

    private static string Html<TOptions>(BotBacktestResult<TOptions> result)
    {
        var sampledEquity = Downsample(result.EquityCurve, MaximumChartPoints);
        var equity = JsonSerializer.Serialize(
            sampledEquity.Select(x => new { t = x.TimeUtc.ToString("O"), e = x.Equity }),
            JsonOptions);
        var metrics = result.Metrics;
        var title = WebUtility.HtmlEncode($"{result.BotName} backtest");

        return $$"""
<!doctype html><html><head><meta charset="utf-8"><title>{{title}}</title>
<style>body{font-family:Arial;background:#10151d;color:#e7edf4;margin:24px}.cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:12px}.card,section{background:#19212c;padding:16px;border-radius:10px;margin-bottom:16px}.label{color:#91a0b3;font-size:12px}.value{font-size:22px;font-weight:bold}svg{width:100%;height:360px;background:#0d1219}</style></head>
<body><h1>{{title}}</h1><div class="cards">
{{Card("Final balance", metrics.FinalBalance)}}{{Card("Net profit", metrics.NetProfit)}}{{Card("Return %", metrics.ReturnPercent)}}{{Card("Win rate %", metrics.WinRatePercent)}}{{Card("Profit factor", metrics.ProfitFactor == decimal.MaxValue ? "Infinity" : metrics.ProfitFactor)}}{{Card("Max DD %", metrics.MaximumDrawdownPercent)}}{{Card("Positions", metrics.ClosedPositions)}}{{Card("Blocked", metrics.BlockedSignals)}}
</div><section><h2>Equity</h2><svg id="equity"></svg></section>
<script>const d={{equity}};const s=document.getElementById('equity'),w=1200,h=340,p=20;if(d.length){const v=d.map(x=>+x.e),mi=Math.min(...v),ma=Math.max(...v),sp=ma-mi||1;const pts=d.map((x,i)=>`${p+i*(w-2*p)/Math.max(1,d.length-1)},${h-p-(+x.e-mi)*(h-2*p)/sp}`).join(' ');s.setAttribute('viewBox',`0 0 ${w} ${h}`);s.innerHTML=`<polyline fill="none" stroke="#65a9ff" stroke-width="2" points="${pts}"/>`;}</script></body></html>
""";
    }

    private static IReadOnlyList<T> Downsample<T>(IReadOnlyList<T> values, int maximum)
    {
        if (values.Count <= maximum)
            return values;

        var result = new List<T>(maximum);
        var step = (values.Count - 1d) / (maximum - 1d);
        for (var index = 0; index < maximum; index++)
            result.Add(values[(int)Math.Round(index * step)]);
        return result;
    }

    private static async Task WriteAtomicAsync(string path, string content, CancellationToken ct)
    {
        var temporaryPath = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await File.WriteAllTextAsync(temporaryPath, content, new UTF8Encoding(false), ct);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static string Csv(params object?[] values)
        => string.Join(',', values.Select(value => Escape(value?.ToString() ?? string.Empty)));

    private static string Escape(string value)
    {
        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
            return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string Card(string label, object value)
        => $"<div class='card'><div class='label'>{WebUtility.HtmlEncode(label)}</div><div class='value'>{WebUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture))}</div></div>";

    private static string F(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Sanitize(string value)
        => string.Concat(value.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
}
