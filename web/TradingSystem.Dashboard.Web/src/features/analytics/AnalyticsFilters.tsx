import type { ReactNode } from 'react'
import { useState } from 'react'
import { Filter, RotateCcw, Search } from 'lucide-react'

import { Button } from '@/components/ui/Button'
import {
  localInputValue,
  toUtc,
} from '@/features/analytics/analytics-formatters'
import type { AnalyticsFilter } from '@/types/analytics'

interface AnalyticsFiltersProps {
  onApply: (filter: AnalyticsFilter) => void
}

function initialDates() {
  const now = new Date()
  const from = new Date(now)
  from.setDate(from.getDate() - 30)

  return {
    from: localInputValue(from),
    to: localInputValue(now),
  }
}

export function AnalyticsFilters({ onApply }: AnalyticsFiltersProps) {
  const dates = initialDates()
  const [botName, setBotName] = useState('')
  const [symbol, setSymbol] = useState('')
  const [fromDate, setFromDate] = useState(dates.from)
  const [toDate, setToDate] = useState(dates.to)

  function apply() {
    onApply({
      botName: botName.trim() || undefined,
      symbol: symbol.trim().toUpperCase() || undefined,
      fromUtc: toUtc(fromDate),
      toUtc: toUtc(toDate),
    })
  }

  function reset() {
    const resetDates = initialDates()
    setBotName('')
    setSymbol('')
    setFromDate(resetDates.from)
    setToDate(resetDates.to)
    onApply({})
  }

  return (
    <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] p-5">
      <div className="mb-4 flex items-center gap-2 text-xs font-medium uppercase tracking-[0.10em] text-[var(--color-text-muted)]">
        <Filter size={14} />
        Analytics filters
      </div>

      <div className="flex items-end gap-3">
        <Field label="Bot">
          <input className={inputClass} value={botName} onChange={(event) => setBotName(event.target.value)} placeholder="All bots"/>
        </Field>

        <Field label="Symbol">
          <input className={inputClass} value={symbol} onChange={(event) => setSymbol(event.target.value)} placeholder="All symbols"/>
        </Field>

        <Field label="From">
          <input className={inputClass} type="datetime-local" value={fromDate} onChange={(event) => setFromDate(event.target.value)} />
        </Field>

        <Field label="To">
          <input className={inputClass} type="datetime-local" value={toDate} onChange={(event) => setToDate(event.target.value)} />
        </Field>

        <div className="ml-auto flex gap-2">
          <Button size="sm" leftIcon={<RotateCcw size={14} />} onClick={reset}>
            Reset
          </Button>

          <Button size="sm" variant="primary" leftIcon={<Search size={14} />} onClick={apply}>
            Apply
          </Button>
        </div>
      </div>
    </div>
  )
}

const inputClass = 'h-9 min-w-[160px] rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-xs text-[var(--color-text-primary)] outline-none transition placeholder:text-[var(--color-text-muted)] focus:border-[#454954]'

function Field({label, children}: { label: string, children: ReactNode}) {
  return (
    <label>
      <span className="mb-2 block text-[11px] text-[var(--color-text-secondary)]">
        {label}
      </span>
      {children}
    </label>
  )
}
