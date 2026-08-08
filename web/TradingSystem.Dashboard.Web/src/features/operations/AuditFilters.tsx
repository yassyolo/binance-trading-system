import { useState } from 'react'
import type { ReactNode } from 'react'
import {
  Filter,
  RotateCcw,
  Search,
} from 'lucide-react'

import { Button } from '@/components/ui/Button'
import {
  localInputValue,
  toUtc,
} from '@/features/operations/operations-formatters'
import type { AuditQuery } from '@/types/operations'

interface AuditFiltersProps {
  onApply: (query: AuditQuery) => void
  take: number
}

function defaultDates() {
  const now = new Date()
  const from = new Date(now)
  from.setDate(from.getDate() - 7)

  return {
    from: localInputValue(from),
    to: localInputValue(now),
  }
}

export function AuditFilters({
  onApply,
  take,
}: AuditFiltersProps) {
  const dates = defaultDates()

  const [actor, setActor] = useState('')
  const [action, setAction] = useState('')
  const [fromDate, setFromDate] =
    useState(dates.from)
  const [toDate, setToDate] =
    useState(dates.to)

  function apply() {
    onApply({
      actor: actor.trim() || undefined,
      action: action.trim() || undefined,
      fromUtc: toUtc(fromDate),
      toUtc: toUtc(toDate),
      skip: 0,
      take,
    })
  }

  function reset() {
    const resetDates = defaultDates()

    setActor('')
    setAction('')
    setFromDate(resetDates.from)
    setToDate(resetDates.to)

    onApply({
      skip: 0,
      take,
    })
  }

  return (
    <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] p-5">
      <div className="mb-4 flex items-center gap-2 text-xs font-medium uppercase tracking-[0.10em] text-[var(--color-text-muted)]">
        <Filter size={14} />
        Audit filters
      </div>

      <div className="flex items-end gap-3">
        <Field label="Actor">
          <input
            className={inputClass}
            value={actor}
            onChange={(event) =>
              setActor(event.target.value)
            }
            placeholder="operator"
          />
        </Field>

        <Field label="Action">
          <input
            className={inputClass}
            value={action}
            onChange={(event) =>
              setAction(event.target.value)
            }
            placeholder="UpdateConfiguration"
          />
        </Field>

        <Field label="From">
          <input
            className={inputClass}
            type="datetime-local"
            value={fromDate}
            onChange={(event) =>
              setFromDate(event.target.value)
            }
          />
        </Field>

        <Field label="To">
          <input
            className={inputClass}
            type="datetime-local"
            value={toDate}
            onChange={(event) =>
              setToDate(event.target.value)
            }
          />
        </Field>

        <div className="ml-auto flex gap-2">
          <Button
            size="sm"
            leftIcon={<RotateCcw size={14} />}
            onClick={reset}
          >
            Reset
          </Button>

          <Button
            size="sm"
            variant="primary"
            leftIcon={<Search size={14} />}
            onClick={apply}
          >
            Apply
          </Button>
        </div>
      </div>
    </div>
  )
}

const inputClass =
  'h-9 min-w-[160px] rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-xs text-[var(--color-text-primary)] outline-none transition placeholder:text-[var(--color-text-muted)] focus:border-[#454954]'

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
