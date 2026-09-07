import type { ReactNode } from 'react'
import { useState } from 'react'
import { CandlestickChart as CandlesIcon, RefreshCcw, Search } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { EmptyState } from '@/components/ui/EmptyState'
import { ErrorState } from '@/components/ui/ErrorState'
import { LoadingSkeleton } from '@/components/ui/LoadingSkeleton'
import { CandlestickChart } from '@/features/analytics/CandlestickChart'
import { localInputValue, toUtc } from '@/features/analytics/analytics-formatters'
import { usePriceChart } from '@/features/analytics/analytics.queries'
import type { PriceChartFilter } from '@/types/analytics'

function initialFilter(): PriceChartFilter {
  const now = new Date()
  const from = new Date(now)
  from.setHours(from.getHours() - 24)

  return {
    symbol: 'BTCUSDC',
    interval: '5m',
    fromUtc: from.toISOString(),
    toUtc: now.toISOString(),
  }
}

export function PriceChartPanel() {
  const [filter, setFilter] = useState<PriceChartFilter>(initialFilter)
  const [symbol, setSymbol] = useState(filter.symbol)
  const [interval, setInterval] = useState(filter.interval)
  const [fromDate, setFromDate] = useState(localInputValue(new Date(filter.fromUtc)))
  const [toDate, setToDate] = useState(localInputValue(new Date(filter.toUtc)))

  const query = usePriceChart(filter)

  function apply() {
    const fromUtc = toUtc(fromDate)
    const toUtcValue = toUtc(toDate)

    if (!fromUtc || !toUtcValue)
      return

    setFilter({
      symbol: symbol.trim().toUpperCase(),
      interval: interval.trim().toLowerCase(),
      fromUtc,
      toUtc: toUtcValue,
    })
  }

  return (
    <Card className="p-5">
      <div className="flex items-start justify-between gap-6">
        <div>
          <div className="flex items-center gap-2">
            <CandlesIcon size={17} className="text-[var(--color-text-secondary)]" />
            <h3 className="text-sm font-semibold">Price chart</h3>
          </div>
          <p className="mt-1 text-xs text-[var(--color-text-muted)]">
            Historical OHLC candles with trading markers from the Dashboard API.
          </p>
        </div>

        <Button size="sm" leftIcon={<RefreshCcw size={14} className={query.isFetching ? 'animate-spin' : ''} />} onClick={() => void query.refetch()} disabled={query.isFetching}>
          Refresh
        </Button>
      </div>

      <div className="mt-6 flex items-end gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)] p-4">
        <Field label="Symbol">
          <input className={inputClass} value={symbol} onChange={(event) => setSymbol(event.target.value)} />
        </Field>

        <Field label="Interval">
          <select className={inputClass} value={interval} onChange={(event) => setInterval(event.target.value)}>
            {['1m', '3m', '5m', '15m', '30m', '1h', '4h', '1d'].map((value) => ( <option key={value} value={value}> {value} </option>))}
          </select>
        </Field>

        <Field label="From">
          <input className={inputClass} type="datetime-local" value={fromDate} onChange={(event) => setFromDate(event.target.value)}/>
        </Field>

        <Field label="To">
          <input className={inputClass} type="datetime-local" value={toDate} onChange={(event) => setToDate(event.target.value)}/>
        </Field>

        <Button className="ml-auto" size="sm" variant="primary" leftIcon={<Search size={14} />} onClick={apply}>
          Load chart
        </Button>
      </div>

      <div className="mt-5">
        {query.isPending && (
          <LoadingSkeleton className="h-[430px] w-full rounded-xl" />
        )}

        {query.isError && (
          <ErrorState
            title="Price chart unavailable"
            description={query.error.message}
            action={<Button onClick={() => void query.refetch()}>Retry</Button>}
          />
        )}

        {query.data && query.data.candles.length === 0 && (
          <EmptyState
            title="No candles found"
            description="No historical candles are available for the selected symbol, interval and time range."
          />
        )}

        {query.data && query.data.candles.length > 0 && (
          <>
            <div className="mb-3 flex items-center justify-between text-xs text-[var(--color-text-muted)]">
              <span>
                {filter.symbol} · {filter.interval}
              </span>
              <span>
                {query.data.candles.length} candles · {query.data.markers.length} markers
              </span>
            </div>

            <CandlestickChart
              candles={query.data.candles}
              markers={query.data.markers}
            />
          </>
        )}
      </div>
    </Card>
  )
}

const inputClass =
  'h-9 min-w-[135px] rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-xs text-[var(--color-text-primary)] outline-none focus:border-[#454954]'

function Field({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <label>
      <span className="mb-2 block text-[11px] text-[var(--color-text-secondary)]">
        {label}
      </span>
      {children}
    </label>
  )
}
