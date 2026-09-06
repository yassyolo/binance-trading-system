import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

import { Card } from '@/components/ui/Card'
import { money } from '@/features/analytics/analytics-formatters'
import type { AnalyticsSummaryDto } from '@/types/analytics'

export function AnalyticsBreakdowns({
  analytics,
}: {
  analytics: AnalyticsSummaryDto
}) {
  const blockReasons = Object.entries(analytics.blockReasons)
    .map(([reason, count]) => ({ reason, count }))
    .sort((left, right) => right.count - left.count)
    .slice(0, 8)

  const sideData = [
    { side: 'Long', pnl: analytics.longPnl },
    { side: 'Short', pnl: analytics.shortPnl },
  ]

  return (
    <div className="grid grid-cols-2 gap-4">
      <Card className="p-5">
        <h3 className="text-sm font-semibold">Signal funnel</h3>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">
          Generated signals and strategy admission result.
        </p>

        <div className="mt-7 space-y-5">
          <FunnelRow label="Signals" value={analytics.signals} max={Math.max(analytics.signals, 1)} />
          <FunnelRow label="Opened" value={analytics.openedSignals} max={Math.max(analytics.signals, 1)} />
          <FunnelRow
            label="Blocked"
            value={analytics.blockedSignals}
            max={Math.max(analytics.signals, 1)}
          />
        </div>

        <div className="mt-8 border-t border-[var(--color-border)] pt-5">
          <p className="text-[10px] font-medium uppercase tracking-[0.10em] text-[var(--color-text-muted)]">
            Top block reasons
          </p>

          {blockReasons.length === 0 ? (
            <p className="mt-4 text-xs text-[var(--color-text-secondary)]">
              No block reasons recorded.
            </p>
          ) : (
            <div className="mt-4 space-y-3">
              {blockReasons.map((item) => (
                <div key={item.reason} className="flex items-center justify-between gap-4">
                  <span className="truncate text-xs text-[var(--color-text-secondary)]" title={item.reason}>
                    {item.reason}
                  </span>
                  <span className="font-mono text-xs text-[var(--color-text-muted)]">
                    {item.count}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      </Card>

      <Card className="p-5">
        <h3 className="text-sm font-semibold">Long vs Short PnL</h3>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">
          Realized result grouped by position direction.
        </p>

        <div className="mt-5 h-[310px]">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={sideData} margin={{ top: 10, right: 8, left: 8, bottom: 0 }}>
              <CartesianGrid
                stroke="var(--color-border)"
                vertical={false}
                strokeDasharray="3 3"
              />
              <XAxis
                dataKey="side"
                axisLine={false}
                tickLine={false}
                tick={{ fill: 'var(--color-text-muted)', fontSize: 11 }}
              />
              <YAxis
                axisLine={false}
                tickLine={false}
                tickFormatter={(value: number) => `$${Math.round(value)}`}
                tick={{ fill: 'var(--color-text-muted)', fontSize: 11 }}
              />
              <Tooltip
                cursor={{ fill: 'var(--color-surface-hover)' }}
                contentStyle={{
                  background: 'var(--color-surface-strong)',
                  border: '1px solid var(--color-border)',
                  borderRadius: 12,
                  fontSize: 12,
                }}
                formatter={(value) => [money(Number(value)), 'PnL']}
              />
              <Bar dataKey="pnl" radius={[6, 6, 0, 0]}>
                {sideData.map((entry) => (
                  <Cell
                    key={entry.side}
                    fill={
                      entry.pnl >= 0
                        ? 'var(--color-success)'
                        : 'var(--color-danger)'
                    }
                  />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      </Card>
    </div>
  )
}

function FunnelRow({
  label,
  value,
  max,
}: {
  label: string
  value: number
  max: number
}) {
  const percent = Math.max(0, Math.min(100, (value / max) * 100))

  return (
    <div>
      <div className="mb-2 flex items-center justify-between">
        <span className="text-xs text-[var(--color-text-secondary)]">{label}</span>
        <span className="font-mono text-xs text-[var(--color-text-primary)]">{value}</span>
      </div>
      <div className="h-2 overflow-hidden rounded-full bg-[var(--color-surface-strong)]">
        <div
          className="h-full rounded-full bg-[var(--color-text-primary)]"
          style={{ width: `${percent}%` }}
        />
      </div>
    </div>
  )
}
