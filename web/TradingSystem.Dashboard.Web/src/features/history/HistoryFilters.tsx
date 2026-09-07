import type { ReactNode } from 'react'
import { Filter, RotateCcw, Search } from 'lucide-react'
import { Button } from '@/components/ui/Button'

export interface HistoryFilterValues {
  botName: string
  symbol: string
  status: string
  fromDate: string
  toDate: string
}

interface HistoryFiltersProps {
  values: HistoryFilterValues
  showStatus?: boolean
  showDates?: boolean
  onChange: (values: HistoryFilterValues) => void
  onApply: () => void
  onReset: () => void
}

export function HistoryFilters({ values, showStatus = false, showDates = false, onChange, onApply, onReset,}: HistoryFiltersProps) {
  return (
    <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] p-5">
      <div className="mb-4 flex items-center gap-2 text-xs font-medium uppercase tracking-[0.10em] text-[var(--color-text-muted)]">
        <Filter size={14} />
        Filters
      </div>

      <div className="flex items-end gap-3">
        <Field label="Bot">
          <input className={inputClass} value={values.botName} onChange={(event) => onChange({ ...values, botName: event.target.value })} placeholder="BOT8012"/>
        </Field>

        <Field label="Symbol">
          <input className={inputClass} value={values.symbol} onChange={(event) => onChange({ ...values, symbol: event.target.value })} placeholder="BTCUSDC"/>
        </Field>

        {showStatus && (
          <Field label="Status">
            <input className={inputClass} value={values.status} onChange={(event) => onChange({ ...values, status: event.target.value })} placeholder="Open" />
          </Field>
        )}

        {showDates && (
          <>
            <Field label="From">
              <input className={inputClass} type="datetime-local" value={values.fromDate} onChange={(event) => onChange({ ...values, fromDate: event.target.value }) } />
            </Field>

            <Field label="To">
              <input className={inputClass} type="datetime-local" value={values.toDate} onChange={(event) => onChange({ ...values, toDate: event.target.value }) } />
            </Field>
          </>
        )}

        <div className="ml-auto flex gap-2">
          <Button size="sm" leftIcon={<RotateCcw size={14} />} onClick={onReset}>
            Reset
          </Button>

          <Button size="sm" variant="primary" leftIcon={<Search size={14} />} onClick={onApply}>
            Apply
          </Button>
        </div>
      </div>
    </div>
  )
}

const inputClass = 'h-9 min-w-[150px] rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-xs text-[var(--color-text-primary)] outline-none transition placeholder:text-[var(--color-text-muted)] focus:border-[#454954]'

function Field({ label, children,}: { label: string, children: ReactNode}) {
  return (
    <label className="block">
      <span className="mb-2 block text-[11px] text-[var(--color-text-secondary)]">
        {label}
      </span>
      {children}
    </label>
  )
}
