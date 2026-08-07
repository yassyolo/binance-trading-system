using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Reporting.Writers;

public sealed class BacktestReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions  =  new(JsonSerializerDefaults.Web) { WriteIndented  =  true };

    public async Task<string> WriteAllAsync(BacktestResult result, string rootOutputDirectory, CancellationToken ct = default)
    {
        var dir  =  Path.Combine(rootOutputDirectory,  Sanitize(result.RunId));
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir,  "result.json"),  JsonSerializer.Serialize(result,  JsonOptions),  ct);
        await File.WriteAllTextAsync(Path.Combine(dir,  "trades.csv"),  BuildTradesCsv(result),  ct);
        await File.WriteAllTextAsync(Path.Combine(dir,  "report.html"),  BuildHtml(result),  ct);
        WriteExcel(result,  Path.Combine(dir,  "report.xlsx"));
        return dir;
    }

    private static string BuildTradesCsv(BacktestResult result)
    {
        var b  =  new StringBuilder("id, symbol, side, entry_time_utc, exit_time_utc, entry_price, exit_price, quantity, exit_reason, gross_pnl, entry_fee, exit_fee, funding, net_pnl, r_multiple\n");
        foreach (var t in result.Trades)
            b.AppendLine(string.Join(", ",  t.Id,  t.Symbol,  t.Side,  t.EntryTimeUtc.ToString("O"),  t.ExitTimeUtc.ToString("O"), 
                F(t.EntryPrice),  F(t.ExitPrice),  F(t.Quantity),  t.ExitReason,  F(t.GrossPnl),  F(t.EntryFee),  F(t.ExitFee),  F(t.FundingCost),  F(t.NetPnl),  F(t.RMultiple)));
        return b.ToString();
    }

    private static string BuildHtml(BacktestResult result)
    {
        var candleData  =  JsonSerializer.Serialize(result.Candles.Select(x  =>  new { t  =  x.OpenTimeUtc.ToString("O"),  o = x.Open, h = x.High, l = x.Low, c = x.Close }),  JsonOptions);
        var equityData  =  JsonSerializer.Serialize(result.EquityCurve.Select(x  =>  new { t = x.TimeUtc.ToString("O"),  b = x.Balance,  d = x.DrawdownPercent }),  JsonOptions);
        var tradeRows  =  string.Join("",  result.Trades.Select(t  =>  $"<tr><td>{t.Id}</td><td>{t.Side}</td><td>{t.EntryTimeUtc:u}</td><td>{t.EntryPrice:F2}</td><td>{t.ExitTimeUtc:u}</td><td>{t.ExitPrice:F2}</td><td>{t.ExitReason}</td><td class = '{(t.NetPnl >= 0 ? "win" : "loss")}'>{t.NetPnl:F2}</td></tr>"));
        return $$"""
<!doctype html><html><head><meta charset = "utf-8"><title>{{result.RunId}}</title>
<style>body{font-family:Arial;margin:24px;background:#10151d;color:#e7edf4}.cards{display:grid;grid-template-columns:repeat(auto-fit, minmax(160px, 1fr));gap:12px}.card, section{background:#19212c;padding:16px;border-radius:10px;margin-bottom:16px}.label{color:#91a0b3;font-size:12px}.value{font-size:22px;font-weight:bold}.win{color:#53d18b}.loss{color:#ff6b78}svg{width:100%;height:360px;background:#0d1219;border-radius:8px}table{width:100%;border-collapse:collapse}th, td{padding:8px;border-bottom:1px solid #2b3746;text-align:right}th:first-child, td:first-child{text-align:left}</style></head>
<body><h1>{{result.Request.StrategyName}} — {{result.Request.Symbol}} {{result.Request.Interval}}</h1>
<div class = "cards">{{Card("Final balance",  result.FinalBalance)}}{{Card("Net profit",  result.Metrics.NetProfit)}}{{Card("Win rate %",  result.Metrics.WinRatePercent)}}{{Card("Profit factor",  result.Metrics.ProfitFactor == decimal.MaxValue ? 999m : result.Metrics.ProfitFactor)}}{{Card("Max DD %",  result.Metrics.MaximumDrawdownPercent)}}{{Card("Trades",  result.Metrics.TotalTrades)}}</div>
<section><h2>Equity curve</h2><svg id = "equity"></svg></section><section><h2>Price candles</h2><svg id = "price"></svg></section>
<section><h2>Trades</h2><table><thead><tr><th>ID</th><th>Side</th><th>Entry time</th><th>Entry</th><th>Exit time</th><th>Exit</th><th>Reason</th><th>Net PnL</th></tr></thead><tbody>{{tradeRows}}</tbody></table></section>
<script>const candles = {{candleData}}, equity = {{equityData}};
function line(svgId, data, key){const s = document.getElementById(svgId), w = 1200, h = 340, p = 20;if(!data.length)return;const vals = data.map(x => +x[key]), min = Math.min(...vals), max = Math.max(...vals), span = max-min || 1;const pts = data.map((x, i) => `${p+i*(w-2*p)/Math.max(1, data.length-1)}, ${h-p-(+x[key]-min)*(h-2*p)/span}`).join(' ');s.setAttribute('viewBox', `0 0 ${w} ${h}`);s.innerHTML = `<polyline fill = "none" stroke = "#65a9ff" stroke-width = "2" points = "${pts}"/>`;}
function candlesSvg(){const s = document.getElementById('price'), w = 1200, h = 340, p = 20;if(!candles.length)return;const lo = Math.min(...candles.map(x => +x.l)), hi = Math.max(...candles.map(x => +x.h)), span = hi-lo || 1, step = (w-2*p)/candles.length, bw = Math.max(1, step*.55);s.setAttribute('viewBox', `0 0 ${w} ${h}`);let out = '';candles.forEach((x, i) => {const X = p+i*step+step/2, y = v => h-p-(+v-lo)*(h-2*p)/span, up = +x.c>=+x.o, col = up?'#53d18b':'#ff6b78', top = Math.min(y(x.o), y(x.c)), bh = Math.max(1, Math.abs(y(x.o)-y(x.c)));out+=`<line x1 = "${X}" y1 = "${y(x.h)}" x2 = "${X}" y2 = "${y(x.l)}" stroke = "${col}"/><rect x = "${X-bw/2}" y = "${top}" width = "${bw}" height = "${bh}" fill = "${col}"/>`;});s.innerHTML = out;} line('equity', equity, 'b');candlesSvg();</script></body></html>
""";
    }

    private static string Card(string label,  object value)  =>  $"<div class = 'card'><div class = 'label'>{label}</div><div class = 'value'>{value}</div></div>";

    private static void WriteExcel(BacktestResult result,  string path)
    {
        using var wb  =  new XLWorkbook();
        var summary  =  wb.Worksheets.Add("Summary");
        var rows  =  new (string,  object)[] { ("Strategy", result.Request.StrategyName), ("Symbol", result.Request.Symbol), ("Initial balance", result.InitialBalance), ("Final balance", result.FinalBalance), ("Net profit", result.Metrics.NetProfit), ("Win rate %", result.Metrics.WinRatePercent), ("Profit factor", result.Metrics.ProfitFactor), ("Max drawdown %", result.Metrics.MaximumDrawdownPercent), ("Total trades", result.Metrics.TotalTrades), ("Total fees", result.Metrics.TotalFees) };
        for (var i = 0;i<rows.Length;i++){summary.Cell(i+1, 1).Value = rows[i].Item1; summary.Cell(i+1, 2).Value = rows[i].Item2.ToString();}
        summary.Columns().AdjustToContents();
        var trades  =  wb.Worksheets.Add("Trades");
        var headers  =  new[]{"Id", "Symbol", "Side", "Entry UTC", "Exit UTC", "Entry", "Exit", "Quantity", "Reason", "Gross PnL", "Entry fee", "Exit fee", "Net PnL", "R"};
        for(var c = 0;c<headers.Length;c++) trades.Cell(1, c+1).Value = headers[c];
        for(var r = 0;r<result.Trades.Count;r++){var t = result.Trades[r];var v = new object[]{t.Id, t.Symbol, t.Side.ToString(), t.EntryTimeUtc, t.ExitTimeUtc, t.EntryPrice, t.ExitPrice, t.Quantity, t.ExitReason.ToString(), t.GrossPnl, t.EntryFee, t.ExitFee, t.NetPnl, t.RMultiple};for(var c = 0;c<v.Length;c++)trades.Cell(r+2, c+1).Value = v[c]?.ToString();}
        trades.Columns().AdjustToContents();
        var eq  =  wb.Worksheets.Add("Equity"); eq.Cell(1, 1).Value = "Time UTC";eq.Cell(1, 2).Value = "Balance";eq.Cell(1, 3).Value = "Drawdown %";
        for(var r = 0;r<result.EquityCurve.Count;r++){eq.Cell(r+2, 1).Value = result.EquityCurve[r].TimeUtc;eq.Cell(r+2, 2).Value = result.EquityCurve[r].Balance;eq.Cell(r+2, 3).Value = result.EquityCurve[r].DrawdownPercent;}
        eq.Columns().AdjustToContents(); wb.SaveAs(path);
    }

    private static string F(decimal value)  =>  value.ToString(CultureInfo.InvariantCulture);
    private static string Sanitize(string value)  =>  string.Concat(value.Select(ch  =>  Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
}
