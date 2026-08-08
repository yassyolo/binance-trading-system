import { useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import {
  FlaskConical,
  Play,
  RotateCcw,
} from 'lucide-react'

import { ApiError } from '@/api/api-error'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { ErrorState } from '@/components/ui/ErrorState'
import {
  ParameterEditor,
  type ParameterRow,
} from '@/features/backtests/ParameterEditor'
import {
  localInputValue,
} from '@/features/backtests/backtest-formatters'
import { useCreateBacktest } from '@/features/backtests/backtests.queries'
import type { JobAcceptedDto } from '@/types/backtesting'

interface CreateBacktestFormProps {
  onAccepted: (job: JobAcceptedDto) => void
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

export function CreateBacktestForm({
  onAccepted,
}: CreateBacktestFormProps) {
  const dates = initialDates()
  const [botName, setBotName] = useState('BOT8012')
  const [symbol, setSymbol] = useState('BTCUSDC')
  const [fromDate, setFromDate] = useState(dates.from)
  const [toDate, setToDate] = useState(dates.to)
  const [initialBalance, setInitialBalance] = useState('10000')
  const [signalSource, setSignalSource] = useState('Internal')
  const [commissionPercent, setCommissionPercent] = useState('0.04')
  const [slippagePercent, setSlippagePercent] = useState('0.01')
  const [parameters, setParameters] = useState<ParameterRow[]>([])

  const mutation = useCreateBacktest()
  const error = mutation.error instanceof ApiError
    ? mutation.error
    : null

  function reset() {
    const resetDates = initialDates()

    setBotName('BOT8012')
    setSymbol('BTCUSDC')
    setFromDate(resetDates.from)
    setToDate(resetDates.to)
    setInitialBalance('10000')
    setSignalSource('Internal')
    setCommissionPercent('0.04')
    setSlippagePercent('0.01')
    setParameters([])
    mutation.reset()
  }

  function submit(event: FormEvent<HTMLFormElement>) {
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

    const parameterMap = parameters.reduce<Record<string, string>>(
      (result, row) => {
        const name = row.name.trim()

        if (name)
          result[name] = row.value.trim()

        return result
      },
      {},
    )

    mutation.mutate(
      {
        botName,
        symbol: symbol.trim().toUpperCase(),
        fromUtc: fromUtc.toISOString(),
        toUtc: toUtc.toISOString(),
        initialBalance: Number(initialBalance),
        signalSource: signalSource.trim(),
        commissionPercent: Number(commissionPercent),
        slippagePercent: Number(slippagePercent),
        parameters: parameterMap,
      },
      {
        onSuccess: onAccepted,
      },
    )
  }

  return (
    <form onSubmit={submit} className="space-y-5">
      {error && (
        <ErrorState
          title="Backtest request failed"
          description={error.message}
        />
      )}

      <Card className="p-6">
        <div className="flex items-start justify-between gap-6">
          <div>
            <div className="flex items-center gap-2">
              <FlaskConical
                size={17}
                className="text-[var(--color-text-secondary)]"
              />
              <h3 className="text-sm font-semibold">
                New backtest
              </h3>
            </div>

            <p className="mt-2 max-w-2xl text-xs leading-5 text-[var(--color-text-muted)]">
              The request is queued and executed asynchronously by TradingSystem.Jobs.Worker.
              Historical candle gaps or missing signals cause the job to fail instead of producing a misleading result.
            </p>
          </div>
        </div>

        <div className="mt-6 grid grid-cols-4 gap-4">
          <Field label="Bot">
            <select
              className={inputClass}
              value={botName}
              onChange={(event) => setBotName(event.target.value)}
            >
              {['BOT8011', 'BOT8012', 'BOT8013', 'BOT8014', 'BOT8015', 'BOT8016'].map(
                (bot) => (
                  <option key={bot} value={bot}>
                    {bot}
                  </option>
                ),
              )}
            </select>
          </Field>

          <Field label="Symbol">
            <input
              className={inputClass}
              value={symbol}
              onChange={(event) => setSymbol(event.target.value)}
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
                setInitialBalance(event.target.value)
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

          <Field label="Commission %">
            <input
              className={inputClass}
              type="number"
              min="0"
              step="any"
              value={commissionPercent}
              onChange={(event) =>
                setCommissionPercent(event.target.value)
              }
              required
            />
          </Field>

          <Field label="Slippage %">
            <input
              className={inputClass}
              type="number"
              min="0"
              step="any"
              value={slippagePercent}
              onChange={(event) =>
                setSlippagePercent(event.target.value)
              }
              required
            />
          </Field>
        </div>

        <div className="mt-7 border-t border-[var(--color-border)] pt-6">
          <ParameterEditor
            rows={parameters}
            onChange={setParameters}
          />
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
            disabled={mutation.isPending}
          >
            {mutation.isPending
              ? 'Queueing…'
              : 'Run backtest'}
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
