import { Plus, Trash2 } from 'lucide-react'

import { Button } from '@/components/ui/Button'

export interface ParameterRow {
  id: string
  name: string
  value: string
}

interface ParameterEditorProps {
  rows: ParameterRow[]
  onChange: (rows: ParameterRow[]) => void
}

export function ParameterEditor({
  rows,
  onChange,
}: ParameterEditorProps) {
  function update(
    id: string,
    field: 'name' | 'value',
    value: string,
  ) {
    onChange(
      rows.map((row) =>
        row.id === id ? { ...row, [field]: value } : row,
      ),
    )
  }

  function add() {
    onChange([
      ...rows,
      {
        id: crypto.randomUUID(),
        name: '',
        value: '',
      },
    ])
  }

  function remove(id: string) {
    onChange(rows.filter((row) => row.id !== id))
  }

  return (
    <div>
      <div className="flex items-center justify-between">
        <div>
          <h4 className="text-xs font-semibold">Strategy parameters</h4>
          <p className="mt-1 text-[11px] text-[var(--color-text-muted)]">
            Optional key/value overrides are applied case-insensitively to the selected bot backtest options.
          </p>
        </div>

        <Button
          size="sm"
          leftIcon={<Plus size={14} />}
          onClick={add}
        >
          Add parameter
        </Button>
      </div>

      {rows.length === 0 ? (
        <div className="mt-4 rounded-xl border border-dashed border-[var(--color-border)] px-4 py-6 text-center text-xs text-[var(--color-text-muted)]">
          No parameter overrides. The backtest engine will use its defaults.
        </div>
      ) : (
        <div className="mt-4 space-y-2">
          {rows.map((row) => (
            <div key={row.id} className="flex gap-2">
              <input
                className={inputClass}
                value={row.name}
                onChange={(event) =>
                  update(row.id, 'name', event.target.value)
                }
                placeholder="Parameter name"
              />

              <input
                className={inputClass}
                value={row.value}
                onChange={(event) =>
                  update(row.id, 'value', event.target.value)
                }
                placeholder="Value"
              />

              <button
                type="button"
                title="Remove parameter"
                aria-label="Remove parameter"
                onClick={() => remove(row.id)}
                className="grid size-10 shrink-0 place-items-center rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] text-[var(--color-text-muted)] transition hover:bg-[var(--color-surface-hover)] hover:text-[var(--color-danger)]"
              >
                <Trash2 size={15} />
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

const inputClass =
  'h-10 w-full rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-sm text-[var(--color-text-primary)] outline-none transition placeholder:text-[var(--color-text-muted)] focus:border-[#454954]'
