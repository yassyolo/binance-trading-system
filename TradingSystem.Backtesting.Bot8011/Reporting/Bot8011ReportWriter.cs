using System.Globalization;
using System.Text;
using System.Text.Json;
using TradingSystem.Backtesting.Bot8011.Models;
using TradingSystem.Backtesting.Bot8011.Optimization;

namespace TradingSystem.Backtesting.Bot8011.Reporting;

public sealed class Bot8011ReportWriter
{
    public async Task<string> WriteAsync(Bot8011BacktestResult result, string outputRoot, CancellationToken ct = default)
    {
        var dir = Path.Combine(outputRoot, result.RunId);
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "result.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }), ct);
        await WritePositionsCsv(result, Path.Combine(dir, "positions.csv"), ct);
        await WriteExecutionsCsv(result, Path.Combine(dir, "executions.csv"), ct);
        await File.WriteAllTextAsync(Path.Combine(dir, "report.html"), BuildHtml(result), ct);
        return dir;
    }

    public async Task WriteOptimizationCsvAsync(IReadOnlyList<Bot8011OptimizationRow> rows, string path, CancellationToken ct = default)
    {
        var sb = new StringBuilder("rank,score,tp_percent,sl_percent,stop3_offset,trailing_step,trailing_buffer,cooldown,positions,win_rate,net_profit,return_percent,profit_factor,max_drawdown_percent,fees\n");
        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i]; var o = r.Options; var m = r.Metrics;
            sb.AppendLine(string.Join(',', i + 1, F(r.Score), F(o.TakeProfitPercent), F(o.StopLossPercent), F(o.Stop3EntryOffset), F(o.Stop3TrailingStep), F(o.Stop3TrailingBuffer), o.CooldownSeconds, m.Positions, F(m.WinRatePercent), F(m.NetProfit), F(m.ReturnPercent), F(m.ProfitFactor), F(m.MaximumDrawdownPercent), F(m.TotalFees)));
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await File.WriteAllTextAsync(path, sb.ToString(), ct);
    }

    private static async Task WritePositionsCsv(Bot8011BacktestResult r, string path, CancellationToken ct)
    {
        var sb = new StringBuilder("id,side,entry_time,entry_price,exit_time,exit_price,quantity,gross_pnl,fees,net_pnl,tp_executed,exit_reason\n");
        foreach (var p in r.Positions) sb.AppendLine($"{p.PositionId},{p.Side},{p.EntryTimeUtc:O},{F(p.EntryPrice)},{p.ExitTimeUtc:O},{F(p.ExitPrice)},{F(p.Quantity)},{F(p.GrossPnl)},{F(p.Fees)},{F(p.NetPnl)},{p.TpExecuted},{p.ExitReason}");
        await File.WriteAllTextAsync(path, sb.ToString(), ct);
    }

    private static async Task WriteExecutionsCsv(Bot8011BacktestResult r, string path, CancellationToken ct)
    {
        var sb = new StringBuilder("position_id,time,type,side,price,quantity,gross_pnl,fee,reason\n");
        foreach (var x in r.Executions) sb.AppendLine($"{x.PositionId},{x.TimeUtc:O},{x.Type},{x.Side},{F(x.Price)},{F(x.Quantity)},{F(x.GrossPnl)},{F(x.Fee)},{x.Reason}");
        await File.WriteAllTextAsync(path, sb.ToString(), ct);
    }

    private static string BuildHtml(Bot8011BacktestResult r)
    {
        var candles = JsonSerializer.Serialize(r.Candles.Select(x => new { t = x.OpenTimeUtc.ToString("O"), o = x.Open, h = x.High, l = x.Low, c = x.Close }));
        var equity = JsonSerializer.Serialize(r.EquityCurve.Select(x => new { t = x.TimeUtc.ToString("O"), b = x.Balance, d = x.DrawdownPercent }));
        var entries = JsonSerializer.Serialize(r.Executions.Where(x => x.Type == "ENTRY").Select(x => new { t = x.TimeUtc.ToString("O"), p = x.Price, side = x.Side.ToString() }));
        var exits = JsonSerializer.Serialize(r.Executions.Where(x => x.Type == "EXIT").Select(x => new { t = x.TimeUtc.ToString("O"), p = x.Price, reason = x.Reason }));
        var m = r.Metrics;
        return $$"""
<!doctype html>
<html>
<head>
    <meta charset="utf-8">
    <title>{{r.RunId}}</title>
    <script src="https://cdn.plot.ly/plotly-2.35.2.min.js"></script>
    <style>
        body{font-family:Arial;background:#111827;color:#e5e7eb;margin:20px}
        .cards{display:grid;grid-template-columns:repeat(6,1fr);gap:10px}
        .card{background:#1f2937;padding:14px;border-radius:8px}
        .card b{display:block;font-size:20px;margin-top:6px}
        #price,#equity{height:520px;margin-top:18px;background:#1f2937}
    </style>
</head>
<body>
    <h1>BOT8011 Backtest</h1>
    <div>{{r.Options.Symbol}} | TP {{r.Options.TakeProfitPercent}}% | SL {{r.Options.StopLossPercent}}% | STOP3 offset {{r.Options.Stop3EntryOffset}}</div>
    <div class="cards">
        <div class="card">Net profit<b>{{m.NetProfit:F2}}</b></div>
        <div class="card">Return<b>{{m.ReturnPercent:F2}}%</b></div>
        <div class="card">Profit factor<b>{{m.ProfitFactor:F2}}</b></div>
        <div class="card">Drawdown<b>{{m.MaximumDrawdownPercent:F2}}%</b></div>
        <div class="card">Positions<b>{{m.Positions}}</b></div>
        <div class="card">Win rate<b>{{m.WinRatePercent:F2}}%</b></div>
    </div>
    <div id="price"></div>
    <div id="equity"></div>
    <script>
        const c={{candles}}, e={{equity}}, en={{entries}}, ex={{exits}};
        Plotly.newPlot('price',[
            {type:'candlestick',x:c.map(x=>x.t),open:c.map(x=>x.o),high:c.map(x=>x.h),low:c.map(x=>x.l),close:c.map(x=>x.c),name:'BTCUSDC'},
            {type:'scatter',mode:'markers',x:en.map(x=>x.t),y:en.map(x=>x.p),text:en.map(x=>x.side),marker:{size:11,symbol:'triangle-up'},name:'Entries'},
            {type:'scatter',mode:'markers',x:ex.map(x=>x.t),y:ex.map(x=>x.p),text:ex.map(x=>x.reason),marker:{size:10,symbol:'x'},name:'Exits'}
        ],{paper_bgcolor:'#1f2937',plot_bgcolor:'#1f2937',font:{color:'#e5e7eb'},xaxis:{rangeslider:{visible:false}},title:'Price, entries and exits'});
        Plotly.newPlot('equity',[
            {type:'scatter',x:e.map(x=>x.t),y:e.map(x=>x.b),name:'Balance'},
            {type:'scatter',x:e.map(x=>x.t),y:e.map(x=>x.d),name:'Drawdown %',yaxis:'y2'}
        ],{paper_bgcolor:'#1f2937',plot_bgcolor:'#1f2937',font:{color:'#e5e7eb'},title:'Equity and drawdown',yaxis2:{overlaying:'y',side:'right'}});
    </script>
</body>
</html>
""";
    }

    private static string F(decimal value) => value.ToString("0.########", CultureInfo.InvariantCulture);
}
