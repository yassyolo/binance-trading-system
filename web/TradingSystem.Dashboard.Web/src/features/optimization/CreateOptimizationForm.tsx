import { useMemo, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import {
  Play,
  RotateCcw,
  SlidersHorizontal,
} from 'lucide-react'

import { ApiError } from '@/api/api-error'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { ErrorState } from '@/components/ui/ErrorState'
import {
  RangeEditor,
  type RangeRow,
} from '@/features/optimization/RangeEditor'
import {
  combinationsForRange,
  localInputValue,
} from '@/features/optimization/optimization-formatters'
import { useCreateOptimization } from '@/features/optimization/optimization.queries'
import type {
  JobAcceptedDto,
  OptimizationRangeDto,
} from '@/types/optimization'

interface CreateOptimizationFormProps {
  onAccepted: (job: JobAcceptedDto) => void
}

function initialDates() {
  const now = new Date()
  const from = new Date(now)
  from.setDate(from.getDate() - 90)

  return {
    from: localInputValue(from),
    to: localInputValue(now),
  }
}

function defaultRanges(): RangeRow[] {
  return [
    {
      id: crypto.randomUUID(),
      name: 'ProfitDistance',
      from: '100',
      to: '300',
      step: '100',
    },
  ]
}

export function CreateOptimizationForm({
  onAccepted,
}: CreateOptimizationFormProps) {
  const dates = initialDates()

  const [botName, setBotName] =
    useState('BOT8012')
  const [symbol, setSymbol] =
    useState('BTCUSDC')
  const [fromDate, setFromDate] =
    useState(dates.from)
  const [toDate, setToDate] =
    useState(dates.to)
  const [initialBalance, setInitialBalance] =
    useState('10000')
  const [signalSource, setSignalSource] =
    useState('Internal')
  const [topResults, setTopResults] =
    useState('10')
  const [walkForward, setWalkForward] =
    useState(false)
  const [trainBars, setTrainBars] =
    useState('1000')
  const [testBars, setTestBars] =
    useState('250')
  const [stepBars, setStepBars] =
    useState('250')
  const [ranges, setRanges] =
    useState<RangeRow[]>(defaultRanges)

  const mutation = useCreateOptimization()
  const error =
    mutation.error instanceof ApiError
      ? mutation.error
      : null

  const combinationCount = useMemo(() => {
    if (ranges.length === 0) return 0

    return ranges.reduce((total, range) => {
      const count = combinationsForRange(
        Number(range.from),
        Number(range.to),
        Number(range.step),
      )

      if (count <= 0) return 0
      return total * count
    }, 1)
  }, [ranges])

  function reset() {
    const resetDates = initialDates()

    setBotName('BOT8012')
    setSymbol('BTCUSDC')
    setFromDate(resetDates.from)
    setToDate(resetDates.to)
    setInitialBalance('10000')
    setSignalSource('Internal')
    setTopResults('10')
    setWalkForward(false)
    setTrainBars('1000')
    setTestBars('250')
    setStepBars('250')
    setRanges(defaultRanges())
    mutation.reset()
  }

  function submit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    const fromUtc = new Date(fromDate)
    const toUtc = new Date(toDate)

    if (
      Number.isNaN(fromUtc.getTime()) ||
      Number.isNaN(toUtc.getTime()) ||
      fromUtc >= toUtc
    ) {
      return
    }

    const rangeDtos: OptimizationRangeDto[] =
      ranges.map((range) => ({
        name: range.name.trim(),
        from: Number(range.from),
        to: Number(range.to),
        step: Number(range.step),
      }))

    mutation.mutate(
      {
        botName,
        symbol: symbol.trim().toUpperCase(),
        fromUtc: fromUtc.toISOString(),
        toUtc: toUtc.toISOString(),
        initialBalance: Number(initialBalance),
        signalSource: signalSource.trim(),
        ranges: rangeDtos,
        topResults: Number(topResults),
        walkForward,
        trainBars: walkForward
          ? Number(trainBars)
          : null,
        testBars: walkForward
          ? Number(testBars)
          : null,
        stepBars: walkForward
          ? Number(stepBars)
          : null,
      },
      {
        onSuccess: onAccepted,
      },
    )
  }

  return (
    <form
      onSubmit={submit}
      className="space-y-5"
    >
      {error && (
        <ErrorState
          title="Optimization request failed"
          description={error.message}
        />
      )}

      <Card className="p-6">
        <div className="flex items-start justify-between gap-6">
          <div>
            <div className="flex items-center gap-2">
              <SlidersHorizontal
                size={17}
                className="text-[var(--color-text-secondary)]"
              />
              <h3 className="text-sm font-semibold">
                New optimization
              </h3>
            </div>

            <p className="mt-2 max-w-3xl text-xs leading-5 text-[var(--color-text-muted)]">
              Each parameter combination is evaluated by the background optimization worker.
              Trial results are persisted individually and can be inspected after the run is created.
            </p>
          </div>

          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)] px-4 py-3 text-right">
            <p className="text-[10px] uppercase tracking-[0.10em] text-[var(--color-text-muted)]">
              Grid combinations
            </p>
            <p className="mt-1 font-mono text-xl font-semibold">
              {combinationCount.toLocaleString('en-US')}
            </p>
          </div>
        </div>

        <div className="mt-6 grid grid-cols-4 gap-4">
          <Field label="Bot">
            <select
              className={inputClass}
              value={botName}
              onChange={(event) =>
                setBotName(event.target.value)
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
                <option key={bot} value={bot}>
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
                setSymbol(event.target.value)
              }
              required
            />
          </Field>

          <Field label="Signal source">
            <input
              className={inputClass}
              value={signalSource}
              onChange={(event) =>
                setSignalSource(event.target.value)
              }
              required
            />
          </Field>

          <Field label="Initial balance">
            <input
              className={inputClass}
              type="number"
              min="0.01"
              step="any"
              value={initialBalance}
              onChange={(event) =>
                setInitialBalance(
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
                setFromDate(event.target.value)
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
                setToDate(event.target.value)
              }
              required
            />
          </Field>

          <Field label="Top results">
            <input
              className={inputClass}
              type="number"
              min="1"
              value={topResults}
              onChange={(event) =>
                setTopResults(event.target.value)
              }
              required
            />
          </Field>

          <div />
        </div>

        <div className="mt-7 border-t border-[var(--color-border)] pt-6">
          <RangeEditor
            rows={ranges}
            onChange={setRanges}
          />
        </div>

        <div className="mt-7 border-t border-[var(--color-border)] pt-6">
          <label className="flex cursor-pointer items-start gap-3">
            <input
              type="checkbox"
              checked={walkForward}
              onChange={(event) =>
                setWalkForward(
                  event.target.checked,
                )
              }
              className="mt-0.5 size-4 accent-white"
            />

            <div>
              <p className="text-sm font-medium">
                Walk-forward validation
              </p>
              <p className="mt-1 text-xs text-[var(--color-text-muted)]">
                Evaluate selected parameters across sequential train/test windows.
              </p>
            </div>
          </label>

          {walkForward && (
            <div className="mt-5 grid max-w-3xl grid-cols-3 gap-4">
              <Field label="Train bars">
                <input
                  className={inputClass}
                  type="number"
                  min="1"
                  value={trainBars}
                  onChange={(event) =>
                    setTrainBars(
                      event.target.value,
                    )
                  }
                  required
                />
              </Field>

              <Field label="Test bars">
                <input
                  className={inputClass}
                  type="number"
                  min="1"
                  value={testBars}
                  onChange={(event) =>
                    setTestBars(
                      event.target.value,
                    )
                  }
                  required
                />
              </Field>

              <Field label="Step bars">
                <input
                  className={inputClass}
                  type="number"
                  min="1"
                  value={stepBars}
                  onChange={(event) =>
                    setStepBars(
                      event.target.value,
                    )
                  }
                  required
                />
              </Field>
            </div>
          )}
        </div>

        <div className="mt-7 flex justify-end gap-2">
          <Button
            leftIcon={<RotateCcw size={15} />}
            onClick={reset}
            disabled={mutation.isPending}
          >
            Reset
          </Button>

          <Button
            type="submit"
            variant="primary"
            leftIcon={<Play size={15} />}
            disabled={
              mutation.isPending ||
              combinationCount <= 0 ||
              ranges.length === 0
            }
          >
            {mutation.isPending
              ? 'Queueing…'
              : 'Start optimization'}
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
