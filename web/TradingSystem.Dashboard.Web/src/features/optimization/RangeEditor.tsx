import { Plus, Trash2 } from 'lucide-react'

import { Button } from '@/components/ui/Button'
import {
  combinationsForRange,
} from '@/features/optimization/optimization-formatters'

export interface RangeRow {
  id: string
  name: string
  from: string
  to: string
  step: string
}

interface RangeEditorProps {
  rows: RangeRow[]
  onChange: (rows: RangeRow[]) => void
}

export function RangeEditor({
  rows,
  onChange,
}: RangeEditorProps) {
  function add() {
    onChange([
      ...rows,
      {
        id: crypto.randomUUID(),
        name: '',
        from: '',
        to: '',
        step: '',
      },
    ])
  }

  function update(
    id: string,
    field: keyof Omit<RangeRow, 'id'>,
    value: string,
  ) {
    onChange(
      rows.map((row) =>
        row.id === id
          ? { ...row, [field]: value }
          : row,
      ),
    )
  }

  function remove(id: string) {
    onChange(rows.filter((row) => row.id !== id))
  }

  return (
    <div>
      <div className="flex items-center justify-between">
        <div>
          <h4 className="text-xs font-semibold">
            Parameter ranges
          </h4>
          <p className="mt-1 text-[11px] text-[var(--color-text-muted)]">
            Each range creates floor((To - From) / Step) + 1 candidates.
          </p>
        </div>

        <Button
          size="sm"
          leftIcon={<Plus size={14} />}
          onClick={add}
        >
          Add range
        </Button>
      </div>

      <div className="mt-4 overflow-hidden rounded-xl border border-[var(--color-border)]">
        <div className="grid grid-cols-[1.6fr_1fr_1fr_1fr_100px_40px] gap-2 border-b border-[var(--color-border)] bg-[var(--color-surface-strong)] px-3 py-2 text-[10px] font-medium uppercase tracking-[0.08em] text-[var(--color-text-muted)]">
          <span>Name</span>
          <span>From</span>
          <span>To</span>
          <span>Step</span>
          <span className="text-right">Candidates</span>
          <span />
        </div>

        {rows.length === 0 ? (
          <div className="px-4 py-8 text-center text-xs text-[var(--color-text-muted)]">
            Add at least one parameter range.
          </div>
        ) : (
          rows.map((row) => {
            const count = combinationsForRange(
              Number(row.from),
              Number(row.to),
              Number(row.step),
            )

            return (
              <div
                key={row.id}
                className="grid grid-cols-[1.6fr_1fr_1fr_1fr_100px_40px] items-center gap-2 border-b border-[var(--color-border)] px-3 py-2 last:border-b-0"
              >
                <input
                  className={inputClass}
                  value={row.name}
                  onChange={(event) =>
                    update(
                      row.id,
                      'name',
                      event.target.value,
                    )
                  }
                  placeholder="ProfitDistance"
                />

                <input
                  className={inputClass}
                  type="number"
                  step="any"
                  value={row.from}
                  onChange={(event) =>
                    update(
                      row.id,
                      'from',
                      event.target.value,
                    )
                  }
                  placeholder="100"
                />

                <input
                  className={inputClass}
                  type="number"
                  step="any"
                  value={row.to}
                  onChange={(event) =>
                    update(
                      row.id,
                      'to',
                      event.target.value,
                    )
                  }
                  placeholder="500"
                />

                <input
                  className={inputClass}
                  type="number"
                  min="0"
                  step="any"
                  value={row.step}
                  onChange={(event) =>
                    update(
                      row.id,
                      'step',
                      event.target.value,
                    )
                  }
                  placeholder="50"
                />

                <span className="text-right font-mono text-xs text-[var(--color-text-secondary)]">
                  {count || '—'}
                </span>

                <button
                  type="button"
                  title="Remove range"
                  aria-label="Remove range"
                  onClick={() => remove(row.id)}
                  className="grid size-8 place-items-center rounded-lg text-[var(--color-text-muted)] transition hover:bg-[var(--color-surface-hover)] hover:text-[var(--color-danger)]"
                >
                  <Trash2 size={14} />
                </button>
              </div>
            )
          })
        )}
      </div>
    </div>
  )
}

const inputClass =
  'h-9 w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-app)] px-2.5 text-xs text-[var(--color-text-primary)] outline-none transition placeholder:text-[var(--color-text-muted)] focus:border-[#454954]'
