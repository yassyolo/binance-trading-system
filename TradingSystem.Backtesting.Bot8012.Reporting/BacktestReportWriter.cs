using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Text.Json;
using TradingSystem.Backtesting.Bot8012.Models;
using TradingSystem.Backtesting.Bot8012.Optimization;
using static System.Net.Mime.MediaTypeNames;
namespace TradingSystem.Backtesting.Bot8012.Reporting;
public static class BacktestReportWriter
{
    public static async Task WriteAsync(Bot8012BacktestResult r, string dir) { Directory.CreateDirectory(dir); var j = new JsonSerializerOptions { WriteIndented = true }; await File.WriteAllTextAsync(Path.Combine(dir, "result.json"), JsonSerializer.Serialize(r, j)); var lines = new List<string> { "id,side,signal_time,entry_time,entry_price,tp_price,exit_time,exit_price,quantity,entry_fee,exit_fee,gross_pnl,net_pnl,exit_reason" }; lines.AddRange(r.Positions.Select(p => string.Join(',', p.Id, p.Side, p.SignalTimeUtc.ToString("O"), p.EntryTimeUtc.ToString("O"), F(p.EntryPrice), F(p.TpPrice), p.ExitTimeUtc?.ToString("O"), F(p.ExitPrice), F(p.Quantity), F(p.EntryFee), F(p.ExitFee), F(p.GrossPnl), F(p.NetPnl), p.ExitReason))); await File.WriteAllLinesAsync(Path.Combine(dir, "positions.csv"), lines); await File.WriteAllTextAsync(Path.Combine(dir, "report.html"), Html(r)); }
    public static async Task WriteOptimizationAsync(IReadOnlyList<OptimizationRow> rows, string dir) { Directory.CreateDirectory(dir); var l = new List<string> { "rank,score,profit_distance,price_distance,side_limit,cooldown,net_profit,profit_factor,max_drawdown,opened,blocked,win_rate" }; l.AddRange(rows.Select((x, i) => string.Join(',', i + 1, F(x.Score), F(x.Options.ProfitDistance), F(x.Options.PriceDistance), x.Options.OrderSideLimit, x.Options.CooldownSeconds, F(x.Metrics.NetProfit), F(x.Metrics.ProfitFactor), F(x.Metrics.MaxDrawdownPercent), x.Metrics.Opened, x.Metrics.Blocked, F(x.Metrics.WinRate)))); await File.WriteAllLinesAsync(Path.Combine(dir, "optimization.csv"), l); }
    private static string F(decimal? v) => v?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Html(Bot8012BacktestResult r) =>
        $@"<!doctype html>
<html>
<head>
    <meta charset='utf-8'>
    <title>BOT8012 Backtest</title>
    <script src='https://cdn.jsdelivr.net/npm/chart.js'></script>
    <style>
        body {{ font-family: Arial; margin: 30px; background: #111; color: #eee; }}
        .cards {{ display: flex; gap: 12px; flex-wrap: wrap; }}
        .card {{ background: #222; padding: 14px; border-radius: 8px; min-width: 150px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 25px; }}
        td, th {{ border-bottom: 1px solid #444; padding: 7px; text-align: right; }}
        th:first-child, td:first-child {{ text-align: left; }}
        canvas {{ background: #fff; margin-top: 25px; }}
    </style>
</head>
<body>
    <h1>BOT8012 Backtest</h1>
    <div class='cards'>
        <div class='card'>Net profit<br><b>{r.Metrics.NetProfit:F2}</b></div>
        <div class='card'>Win rate<br><b>{r.Metrics.WinRate:F2}%</b></div>
        <div class='card'>Profit factor<br><b>{r.Metrics.ProfitFactor:F2}</b></div>
        <div class='card'>Max drawdown<br><b>{r.Metrics.MaxDrawdownPercent:F2}%</b></div>
        <div class='card'>Opened / blocked<br><b>{r.Metrics.Opened} / {r.Metrics.Blocked}</b></div>
    </div>
    <canvas id='eq'></canvas>
    <table>
        <tr>
            <th>ID</th>
            <th>Side</th>
            <th>Entry</th>
            <th>TP</th>
            <th>Exit</th>
            <th>Net PnL</th>
            <th>Reason</th>
        </tr>
        {string.Join("", r.Positions.Select(p =>
            $"<tr><td>{p.Id}</td><td>{p.Side}</td><td>{p.EntryPrice:F2}</td><td>{p.TpPrice:F2}</td><td>{p.ExitPrice:F2}</td><td>{p.NetPnl:F2}</td><td>{p.ExitReason}</td></tr>"
        ))}
    </table>
    <script>
        new Chart(document.getElementById('eq'), {{
            type: 'line',
            data: {{
                labels: {JsonSerializer.Serialize(r.Equity.Select(x => x.TimeUtc.ToString("O")))},
                datasets: [{{ label: 'Equity', data: {JsonSerializer.Serialize(r.Equity.Select(x => x.Equity))} }}]
            }},
            options: {{ animation: false }}
        }});
    </script>
</body>
</html>";
}
