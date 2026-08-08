import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

import { Card } from '@/components/ui/Card'
import {
  compactDate,
  money,
  percent,
} from '@/features/analytics/analytics-formatters'
import type { EquityPointDto } from '@/types/analytics'

interface EquityChartProps {
  points: EquityPointDto[]
}

export function EquityChart({ points }: EquityChartProps) {
  const data = points.map((point) => ({
    time: point.timeUtc,
    equity: point.equity,
    drawdown: -Math.abs(point.drawdownPercent),
  }))

  return (
    <Card className="p-5">
      <div className="mb-5 flex items-start justify-between">
        <div>
          <h3 className="text-sm font-semibold">Equity curve</h3>
          <p className="mt-1 text-xs text-[var(--color-text-muted)]">
            Portfolio equity with drawdown shown below zero.
          </p>
        </div>

        <div className="flex gap-4 text-[11px] text-[var(--color-text-muted)]">
          <span>Equity</span>
          <span>Drawdown</span>
        </div>
      </div>

      <div className="h-[340px]">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={data} margin={{ top: 8, right: 8, left: 4, bottom: 0 }}>
            <defs>
              <linearGradient id="equityFill" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="currentColor" stopOpacity={0.22} />
                <stop offset="100%" stopColor="currentColor" stopOpacity={0} />
              </linearGradient>
            </defs>

            <CartesianGrid
              stroke="var(--color-border)"
              vertical={false}
              strokeDasharray="3 3"
            />

            <XAxis
              dataKey="time"
              tickFormatter={compactDate}
              minTickGap={70}
              axisLine={false}
              tickLine={false}
              tick={{ fill: 'var(--color-text-muted)', fontSize: 11 }}
            />

            <YAxis
              yAxisId="equity"
              orientation="left"
              axisLine={false}
              tickLine={false}
              width={72}
              tickFormatter={(value: number) => `$${Math.round(value)}`}
              tick={{ fill: 'var(--color-text-muted)', fontSize: 11 }}
            />

            <YAxis
              yAxisId="drawdown"
              orientation="right"
              axisLine={false}
              tickLine={false}
              width={48}
              tickFormatter={(value: number) => `${value}%`}
              tick={{ fill: 'var(--color-text-muted)', fontSize: 11 }}
            />

            <Tooltip
              contentStyle={{
                background: 'var(--color-surface-strong)',
                border: '1px solid var(--color-border)',
                borderRadius: 12,
                fontSize: 12,
              }}
              labelFormatter={(value) => compactDate(String(value))}
              formatter={(value, name) => {
                const numericValue = Number(value)

                return name === 'equity'
                  ? [money(numericValue), 'Equity']
                  : [percent(Math.abs(numericValue)), 'Drawdown']
              }}
            />

            <Area
              yAxisId="equity"
              type="monotone"
              dataKey="equity"
              stroke="var(--color-text-primary)"
              fill="url(#equityFill)"
              strokeWidth={1.8}
              dot={false}
              activeDot={{ r: 3 }}
            />

            <Area
              yAxisId="drawdown"
              type="monotone"
              dataKey="drawdown"
              stroke="var(--color-danger)"
              fill="transparent"
              strokeWidth={1.2}
              dot={false}
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </Card>
  )
}
