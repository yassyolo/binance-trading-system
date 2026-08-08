import {
  useMemo,
  useState,
} from 'react'
import type {
  FormEvent,
  ReactNode,
} from 'react'
import {
  History,
  Play,
  RotateCcw,
} from 'lucide-react'

import { ApiError } from '@/api/api-error'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { ErrorState } from '@/components/ui/ErrorState'
import {
  localInputValue,
} from '@/features/replays/replay-formatters'
import {
  useCreateReplay,
} from '@/features/replays/replays.queries'
import type {
  ReplayAcceptedDto,
  ReplayMode,
} from '@/types/replays'

interface CreateReplayFormProps {
  onAccepted: (
    replay: ReplayAcceptedDto,
  ) => void
}

function defaultDates() {
  const now = new Date()
  const from = new Date(now)

  from.setHours(
    from.getHours() - 6,
  )

  return {
    from: localInputValue(from),
    to: localInputValue(now),
  }
}

export function CreateReplayForm({
  onAccepted,
}: CreateReplayFormProps) {
  const dates = useMemo(
    () => defaultDates(),
    [],
  )

  const [name, setName] =
    useState('Dashboard replay')

  const [mode, setMode] =
    useState<ReplayMode>('Timeline')

  const [botName, setBotName] =
    useState('BOT8012')

  const [symbol, setSymbol] =
    useState('BTCUSDC')

  const [fromDate, setFromDate] =
    useState(dates.from)

  const [toDate, setToDate] =
    useState(dates.to)

  const [batchSize, setBatchSize] =
    useState('500')

  const [
    stopOnError,
    setStopOnError,
  ] = useState(true)

  const mutation = useCreateReplay()

  const error =
    mutation.error instanceof ApiError
      ? mutation.error
      : null

  function reset() {
    const resetDates =
      defaultDates()

    setName('Dashboard replay')
    setMode('Timeline')
    setBotName('BOT8012')
    setSymbol('BTCUSDC')
    setFromDate(resetDates.from)
    setToDate(resetDates.to)
    setBatchSize('500')
    setStopOnError(true)
    mutation.reset()
  }

  function submit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    const fromUtc =
      new Date(fromDate)

    const toUtc =
      new Date(toDate)

    const parsedBatchSize =
      Number(batchSize)

    if (
      !name.trim() ||
      Number.isNaN(
        fromUtc.getTime(),
      ) ||
      Number.isNaN(
        toUtc.getTime(),
      ) ||
      fromUtc >= toUtc ||
      !Number.isInteger(
        parsedBatchSize,
      ) ||
      parsedBatchSize <= 0
    ) {
      return
    }

    mutation.mutate(
      {
        name: name.trim(),
        mode,
        fromGlobalPosition: null,
        toGlobalPosition: null,
        fromUtc:
          fromUtc.toISOString(),
        toUtc:
          toUtc.toISOString(),
        botName:
          botName.trim() || null,
        symbol:
          symbol
            .trim()
            .toUpperCase() ||
          null,
        correlationId: null,
        candidateStrategyPluginId:
          null,
        candidateStrategyVersion:
          null,
        batchSize:
          parsedBatchSize,
        stopOnError,
      },
      {
        onSuccess:
          onAccepted,
      },
    )
  }

  return (
    <form onSubmit={submit}>
      {error && (
        <div className="mb-5">
          <ErrorState
            title="Replay request failed"
            description={
              error.message
            }
          />
        </div>
      )}

      <Card className="p-6">
        <div className="flex items-start justify-between gap-6">
          <div>
            <div className="flex items-center gap-2">
              <History
                size={17}
                className="text-[var(--color-text-secondary)]"
              />
              <h3 className="text-sm font-semibold">
                New replay
              </h3>
            </div>

            <p className="mt-2 max-w-3xl text-xs leading-5 text-[var(--color-text-muted)]">
              Reprocess persisted historical events through the replay engine without sending live Binance orders.
            </p>
          </div>
        </div>

        <div className="mt-6 grid grid-cols-4 gap-4">
          <Field label="Name">
            <input
              className={inputClass}
              value={name}
              onChange={(event) =>
                setName(
                  event.target.value,
                )
              }
              required
            />
          </Field>

          <Field label="Mode">
            <select
              className={inputClass}
              value={String(mode)}
              onChange={(event) =>
                setMode(
                  event.target
                    .value as ReplayMode,
                )
              }
            >
              <option value="Timeline">
                Timeline
              </option>
              <option value="Projection">
                Projection
              </option>
              <option value="StrategyComparison">
                Strategy comparison
              </option>
            </select>
          </Field>

          <Field label="Bot">
            <select
              className={inputClass}
              value={botName}
              onChange={(event) =>
                setBotName(
                  event.target.value,
                )
              }
            >
              {[
                'BOT8011',
                'BOT8012',
                'BOT8013',
                'BOT8014',
                'BOT8015',
                'BOT8016',
              ].map((bot) => (
                <option
                  key={bot}
                  value={bot}
                >
                  {bot}
                </option>
              ))}
            </select>
          </Field>

          <Field label="Symbol">
            <input
              className={inputClass}
              value={symbol}
              onChange={(event) =>
                setSymbol(
                  event.target.value,
                )
              }
              required
            />
          </Field>
        </div>

        <div className="mt-4 grid grid-cols-4 gap-4">
          <Field label="From">
            <input
              className={inputClass}
              type="datetime-local"
              value={fromDate}
              onChange={(event) =>
                setFromDate(
                  event.target.value,
                )
              }
              required
            />
          </Field>

          <Field label="To">
            <input
              className={inputClass}
              type="datetime-local"
              value={toDate}
              onChange={(event) =>
                setToDate(
                  event.target.value,
                )
              }
              required
            />
          </Field>

          <Field label="Batch size">
            <input
              className={inputClass}
              type="number"
              min="1"
              step="1"
              value={batchSize}
              onChange={(event) =>
                setBatchSize(
                  event.target.value,
                )
              }
              required
            />
          </Field>

          <label className="flex h-[62px] items-end">
            <span className="flex h-10 w-full items-center gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-xs text-[var(--color-text-secondary)]">
              <input
                type="checkbox"
                checked={stopOnError}
                onChange={(event) =>
                  setStopOnError(
                    event.target.checked,
                  )
                }
              />
              Stop on error
            </span>
          </label>
        </div>

        <div className="mt-7 flex justify-end gap-2">
          <Button
            leftIcon={
              <RotateCcw
                size={15}
              />
            }
            onClick={reset}
            disabled={
              mutation.isPending
            }
          >
            Reset
          </Button>

          <Button
            type="submit"
            variant="primary"
            leftIcon={
              <Play size={15} />
            }
            disabled={
              mutation.isPending
            }
          >
            {mutation.isPending
              ? 'Queueing…'
              : 'Start replay'}
          </Button>
        </div>
      </Card>
    </form>
  )
}

const inputClass =
  'h-10 w-full rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-sm text-[var(--color-text-primary)] outline-none transition placeholder:text-[var(--color-text-muted)] focus:border-[#454954]'

function Field({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <label>
      <span className="mb-2 block text-xs font-medium text-[var(--color-text-secondary)]">
        {label}
      </span>
      {children}
    </label>
  )
}
