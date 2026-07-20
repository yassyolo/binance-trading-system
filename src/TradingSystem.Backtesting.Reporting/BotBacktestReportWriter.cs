using System.Globalization;
using System.Text;
using System.Text.Json;
using TradingSystem.Backtesting.Bots.Common;

namespace TradingSystem.Backtesting.Reporting;

public sealed class BotBacktestReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions  =  new(JsonSerializerDefaults.Web) { WriteIndented  =  true };

    public async Task<string> WriteAsync<TOptions>(BotBacktestResult<TOptions> result,  string outputRoot,  CancellationToken cancellationToken  =  default)
    {
        var directory  =  Path.Combine(outputRoot,  result.RunId);
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory,  "result.json"),  JsonSerializer.Serialize(result,  JsonOptions),  cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(directory,  "positions.csv"),  PositionsCsv(result),  cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(directory,  "executions.csv"),  ExecutionsCsv(result),  cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(directory,  "decisions.csv"),  DecisionsCsv(result),  cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(directory,  "report.html"),  Html(result),  cancellationToken);
        return directory;
    }

    private static string PositionsCsv<TOptions>(BotBacktestResult<TOptions> result)
    {
        var b  =  new StringBuilder("id, side, entry_time, entry_price, exit_time, exit_price, quantity, gross_pnl, fees, net_pnl, partial_tp, exit_reason\n");
        foreach (var p in result.Positions)
            b.AppendLine(string.Join(", ",  p.PositionId,  p.Side,  p.EntryTimeUtc.ToString("O"),  F(p.EntryPrice),  p.ExitTimeUtc.ToString("O"),  F(p.ExitPrice),  F(p.Quantity),  F(p.GrossPnl),  F(p.Fees),  F(p.NetPnl),  p.PartialTakeProfitReached,  p.ExitReason));
        return b.ToString();
    }

    private static string ExecutionsCsv<TOptions>(BotBacktestResult<TOptions> result)
    {
        var b  =  new StringBuilder("position_id, time, type, side, price, quantity, gross_pnl, fee, reason\n");
        foreach (var x in result.Executions)
            b.AppendLine(string.Join(", ",  x.PositionId,  x.TimeUtc.ToString("O"),  x.Type,  x.Side,  F(x.Price),  F(x.Quantity),  F(x.GrossPnl),  F(x.Fee),  x.Reason));
        return b.ToString();
    }

    private static string DecisionsCsv<TOptions>(BotBacktestResult<TOptions> result)
    {
        var b  =  new StringBuilder("time, side, decision, reason, reference_price, signal_id\n");
        foreach (var x in result.Decisions)
            b.AppendLine(string.Join(", ",  x.TimeUtc.ToString("O"),  x.Side,  x.Decision,  Quote(x.Reason),  F(x.ReferencePrice),  x.SignalId));
        return b.ToString();
    }

    private static string Html<TOptions>(BotBacktestResult<TOptions> result)
    {
        var equity  =  JsonSerializer.Serialize(result.EquityCurve.Select(x  =>  new { t  =  x.TimeUtc.ToString("O"),  e  =  x.Equity }),  JsonOptions);
        var m  =  result.Metrics;
        return $$"""
<!doctype html><html><head><meta charset = "utf-8"><title>{{result.RunId}}</title>
<style>body{font-family:Arial;background:#10151d;color:#e7edf4;margin:24px}.cards{display:grid;grid-template-columns:repeat(auto-fit, minmax(150px, 1fr));gap:12px}.card, section{background:#19212c;padding:16px;border-radius:10px;margin-bottom:16px}.label{color:#91a0b3;font-size:12px}.value{font-size:22px;font-weight:bold}svg{width:100%;height:360px;background:#0d1219}</style></head>
<body><h1>{{result.BotName}} backtest</h1><div class = "cards">
{{Card("Final balance",  m.FinalBalance)}}{{Card("Net profit",  m.NetProfit)}}{{Card("Return %",  m.ReturnPercent)}}{{Card("Win rate %",  m.WinRatePercent)}}{{Card("Profit factor",  m.ProfitFactor == decimal.MaxValue ? 999 : m.ProfitFactor)}}{{Card("Max DD %",  m.MaximumDrawdownPercent)}}{{Card("Positions",  m.ClosedPositions)}}{{Card("Blocked",  m.BlockedSignals)}}
</div><section><h2>Equity</h2><svg id = "equity"></svg></section>
<script>const d = {{equity}};const s = document.getElementById('equity'), w = 1200, h = 340, p = 20;if(d.length){const v = d.map(x => +x.e), mi = Math.min(...v), ma = Math.max(...v), sp = ma-mi || 1;const pts = d.map((x, i) => `${p+i*(w-2*p)/Math.max(1, d.length-1)}, ${h-p-(+x.e-mi)*(h-2*p)/sp}`).join(' ');s.setAttribute('viewBox', `0 0 ${w} ${h}`);s.innerHTML = `<polyline fill = "none" stroke = "#65a9ff" stroke-width = "2" points = "${pts}"/>`;}</script></body></html>
""";
    }

    private static string Card(string label,  object value)  =>  $"<div class = 'card'><div class = 'label'>{label}</div><div class = 'value'>{value}</div></div>";
    private static string F(decimal value)  =>  value.ToString(CultureInfo.InvariantCulture);
    private static string Quote(string value)  =>  $"\"{value.Replace("\"",  "\"\"")}\"";
}
